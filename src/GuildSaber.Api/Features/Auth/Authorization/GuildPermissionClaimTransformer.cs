using System.Security.Claims;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Auth.Authorization;

/// <summary>
/// Hydrates the current user's claims with guild permissions based on their player ID.
/// </summary>
public class GuildPermissionClaimTransformer(ServerDbContext dbContext) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if ((!principal.Identity?.IsAuthenticated ?? true) || principal.Identity is not ClaimsIdentity claimsIdentity)
            return principal;

        if (claimsIdentity.HasClaim(c => c.Type.StartsWith(AuthConstants.GuildPermissionClaimPrefix)))
            return principal; // Already transformed

        var playerIdClaim = claimsIdentity.FindFirst(AuthConstants.PlayerIdClaimType);
        if (playerIdClaim == null || !PlayerId.TryParse(playerIdClaim.Value, out var playerId))
            return principal;

        var memberPermissions = await dbContext.Members
            .Where(m => m.PlayerId == playerId)
            .Select(m => new { m.GuildId, m.Permissions })
            .ToListAsync();

        foreach (var perm in memberPermissions)
            claimsIdentity.AddClaim(new Claim(
                AuthConstants.GuildPermissionClaimType(perm.GuildId.Value.ToString()),
                ((uint)perm.Permissions).ToString())
            );

        return principal;
    }
}