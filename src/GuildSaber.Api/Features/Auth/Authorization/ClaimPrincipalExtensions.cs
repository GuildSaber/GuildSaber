using System.Security.Claims;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the player ID from the claims principal
    /// </summary>
    /// <param name="claimsPrincipal">The claims principal containing player identity information</param>
    /// <returns>The player ID if found, null otherwise</returns>
    public static PlayerId? GetPlayerId(this ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity is not ClaimsIdentity identity)
            return null;

        var playerIdClaim = identity.FindFirst(AuthConstants.PlayerIdClaimType);
        if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out var playerId))
            return null;

        return new PlayerId(playerId);
    }
}