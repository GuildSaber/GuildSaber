using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.Auth.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <param name="claimsPrincipal">The claims principal containing player identity information</param>
    extension(ClaimsPrincipal claimsPrincipal)
    {
        /// <summary>
        /// Gets the player ID from the claims principal
        /// </summary>
        /// <returns>The player ID if found, null otherwise</returns>
        public PlayerId? GetPlayerId()
        {
            if (claimsPrincipal.Identity is not ClaimsIdentity identity)
                return null;

            var playerIdClaim = identity.FindFirst(AuthConstants.PlayerIdClaimType);
            if (playerIdClaim == null || !int.TryParse(playerIdClaim.Value, out var playerId))
                return null;

            return new PlayerId(playerId);
        }

        /// <summary>
        /// Gets the session ID from the claims principal
        /// </summary>
        /// <returns>The session ID if found, null otherwise</returns>
        public UuidV7? GetSessionId()
        {
            if (claimsPrincipal.Identity is not ClaimsIdentity identity)
                return null;

            var sessionIdClaim = identity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (sessionIdClaim == null || !Guid.TryParse(sessionIdClaim, out var sessionGuid))
                return null;

            return UuidV7.CreateUnsafe(sessionGuid);
        }

        public bool IsManager() => claimsPrincipal.IsInRole(AuthConstants.ManagerRole);
    }
}