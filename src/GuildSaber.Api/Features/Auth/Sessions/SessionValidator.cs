using System.Security.Claims;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Auth.Sessions;

public class SessionValidator(ServerDbContext dbContext)
{
    private readonly record struct SessionLightDto(UuidV7 SessionId, PlayerId PlayerId, bool IsValid);

    /// <summary>
    /// Validates session existence and validity in database.
    /// If valid, enriches the principal with a claim containing the PlayerId.
    /// </summary>
    /// <param name="sessionId">Session identifier to validate</param>
    /// <param name="principal">Principal to enrich if session is valid</param>
    /// <remarks>
    /// It doesn't checks for the session time because the jwt should have been checked by its authentication handler.
    /// </remarks>
    /// <returns>True if session is valid, otherwise False</returns>
    public async Task<UnitResult<string>> ValidateSessionAsync(UuidV7 sessionId, ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return Failure("Expected SessionPrincipal.Identity to be ClaimsIdentity");

        var session = await dbContext.Sessions.Where(x => x.SessionId == sessionId)
            .Select(s => new SessionLightDto(s.SessionId, s.PlayerId, s.IsValid))
            .FirstOrDefaultAsync();

        if (session == default)
            return Failure("Invalid session");

        if (!session.IsValid)
            return Failure("Session is not valid");

        var isManager = await dbContext.Players
            .Where(x => x.Id == session.PlayerId)
            .Select(x => x.IsManager)
            .FirstOrDefaultAsync();

        if (isManager) identity.AddClaim(new Claim(ClaimTypes.Role, AuthConstants.ManagerRole));

        identity.AddClaim(new Claim(AuthConstants.PlayerIdClaimType, session.PlayerId.ToString()));
        return Success();
    }
}