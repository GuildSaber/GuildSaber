using System.Security.Claims;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Auth.Authorization;

/// <summary>
/// Hydrates the current user's claims with guild permissions based on their player ID.
/// Added claims will be of the form:
/// <list type="bullet">
///     <item>
///         <description>GuildPermissionClaimType(guildId)</description>
///     </item>
///     <item>
///         <description>PlayerId</description>
///     </item>
///     <item>
///         <description>Manager?</description>
///     </item>
/// </list>
/// </summary>
/// <param name="dbContext"></param>
public class PermissionClaimTransformer(ServerDbContext dbContext) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if ((!principal.Identity?.IsAuthenticated ?? true) || principal.Identity is not ClaimsIdentity claimsIdentity)
            return principal;

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