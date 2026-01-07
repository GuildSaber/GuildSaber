using System.Security.Claims;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.Api.Features.Auth.Authorization;

/// <summary>
/// Hydrates the current user's claims with guild permissions based on their player ID.
/// </summary>
public class GuildPermissionClaimTransformer(IServiceScopeFactory scopeFactory, HybridCache cache)
    : IClaimsTransformation
{
    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(2)
    };

    private static readonly Func<ServerDbContext, PlayerId, IAsyncEnumerable<PlayerGuildPermissions>>
        _getMemberPermissionsByPlayerIdQuery = EF.CompileAsyncQuery((ServerDbContext dbContext, PlayerId playerId) =>
            dbContext.Members
                .Where(m => m.PlayerId == playerId)
                .Select(m => new PlayerGuildPermissions(m.GuildId, m.Permissions)));

    private static readonly Func<ServerDbContext, PlayerId, Task<bool>> _isPlayerManagerQuery
        = EF.CompileAsyncQuery((ServerDbContext dbContext, PlayerId playerId) =>
            dbContext.Players
                .Where(x => x.Id == playerId)
                .Select(x => x.IsManager)
                .FirstOrDefault());

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if ((!principal.Identity?.IsAuthenticated ?? true) || principal.Identity is not ClaimsIdentity claimsIdentity)
            return principal;

        if (claimsIdentity.HasClaim(c => c.Type.StartsWith(AuthConstants.GuildPermissionClaimPrefix)))
            return principal; // Already transformed

        var playerIdClaim = claimsIdentity.FindFirst(AuthConstants.PlayerIdClaimType);
        if (playerIdClaim == null || !PlayerId.TryParse(playerIdClaim.Value, out var playerId))
            return principal;

        if (await IsPlayerManagerAsync(playerId))
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, AuthConstants.ManagerRole));

        foreach (var perm in await GetMemberPermissionsByPlayerId(playerId))
            claimsIdentity.AddClaim(new Claim(
                AuthConstants.GuildPermissionClaimType(perm.GuildId.Value.ToString()),
                ((int)perm.Permissions).ToString())
            );

        return principal;
    }

    private readonly record struct PlayerGuildPermissions(GuildId GuildId, EPermission Permissions);

    private ValueTask<PlayerGuildPermissions[]> GetMemberPermissionsByPlayerId(PlayerId playerId)
        => cache.GetOrCreateAsync($"PlayerGuildPermissions_{playerId}", (scopeFactory, playerId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getMemberPermissionsByPlayerIdQuery(dbContext, state.playerId)
                    .ToArrayAsync(token);
            }, _cacheEntryOptions);

    private ValueTask<bool> IsPlayerManagerAsync(PlayerId playerId)
        => cache.GetOrCreateAsync($"IsPlayerManager_{playerId}", (scopeFactory, playerId),
            async static (state, _) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _isPlayerManagerQuery(dbContext, state.playerId);
            }, _cacheEntryOptions);
}