using System.Net;
using System.Security.Claims;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.CustomApiKey.ValidationTypes;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Auth.CustomApiKey;

public class CustomApiKeyAuthenticationService(
    IOptions<ApiKeyAuthSettings> settings,
    IServiceScopeFactory scopeFactory,
    HybridCache cache)
    : ICustomApiKeyAuthenticationService
{
    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(2)
    };

    private static readonly Func<ServerDbContext, DiscordId, Task<PlayerIdWithManagerFlag>>
        _getPlayerIdWithManagerFlagByDiscordIdQueryAsync =
            EF.CompileAsyncQuery((ServerDbContext dbContext, DiscordId discordId) =>
                dbContext.Players
                    .Where(x => x.LinkedAccounts.DiscordId == discordId)
                    .Select(x => new PlayerIdWithManagerFlag(x.Id, x.IsManager))
                    .FirstOrDefault());

    public async Task<AuthenticateResult> AuthenticateAsync(BasicCredential credential, IPAddress? clientIp)
    {
        if (!settings.Value.Key.Equals(credential.Password, StringComparison.Ordinal))
            return AuthenticateResult.Fail("Invalid API key.");

        if (!DiscordId.TryParse(credential.User).TryGetValue(out var discordId))
            return AuthenticateResult.Fail("Invalid Discord ID format.");

        var player = await GetPlayerIdWithManagerFlagByDiscordId(discordId);
        if (player == default)
            return AuthenticateResult.Fail("No player associated with the provided Discord ID.");

        var identity = new ClaimsIdentity(
            [new Claim(AuthConstants.PlayerIdClaimType, player.PlayerId.ToString())],
            BasicAuthenticationDefaults.AuthenticationScheme
        );

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), BasicAuthenticationDefaults.AuthenticationScheme)
        );
    }

    private readonly record struct PlayerIdWithManagerFlag(PlayerId PlayerId, bool IsManager);

    private ValueTask<PlayerIdWithManagerFlag> GetPlayerIdWithManagerFlagByDiscordId(DiscordId discordId)
        => cache.GetOrCreateAsync($"PlayerIdByDiscordId_{discordId}", (scopeFactory, discordId),
            async static (state, _) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getPlayerIdWithManagerFlagByDiscordIdQueryAsync(dbContext, state.discordId);
            }, _cacheEntryOptions);
}