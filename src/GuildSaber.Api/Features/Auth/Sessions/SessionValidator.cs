using System.Security.Claims;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.Api.Features.Auth.Sessions;

public class SessionValidator(IServiceScopeFactory scopeFactory, HybridCache cache)
{
    private const string SessionCacheKeyPrefix = "Session_";

    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(2)
    };

    public static ValueTask ClearSessionCache(UuidV7 sessionId, HybridCache cache)
        => cache.RemoveAsync($"{SessionCacheKeyPrefix}{sessionId}");

    private static readonly Func<ServerDbContext, UuidV7, Task<SessionLightDto>> _getSessionByIdQuery
        = EF.CompileAsyncQuery((ServerDbContext dbContext, UuidV7 sessionId) =>
            dbContext.Sessions
                .Where(x => x.SessionId == sessionId)
                .Select(s => new SessionLightDto(s.SessionId, s.PlayerId, s.IsValid))
                .FirstOrDefault());

    private readonly record struct SessionLightDto(UuidV7 SessionId, PlayerId PlayerId, bool IsValid);

    private ValueTask<SessionLightDto> GetSessionByIdAsync(UuidV7 sessionId)
        => cache.GetOrCreateAsync($"{SessionCacheKeyPrefix}{sessionId}", (scopeFactory, sessionId),
            async static (state, _) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await _getSessionByIdQuery(dbContext, state.sessionId);
            }, _cacheEntryOptions);

    /// <summary>
    /// Validates session existence and validity in database.
    /// If valid, enriches the principal with a claim containing the PlayerId.
    /// </summary>
    /// <param name="sessionId">Session identifier to validate</param>
    /// <param name="principal">Principal to enrich if session is valid</param>
    /// <remarks>
    /// It doesn't check for the session time because the jwt should have been checked by its authentication handler.
    /// </remarks>
    /// <returns>True if session is valid, otherwise False</returns>
    public async Task<UnitResult<string>> ValidateAndApplySessionAsync(UuidV7 sessionId, ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return Failure("Expected SessionPrincipal.Identity to be ClaimsIdentity");

        var session = await GetSessionByIdAsync(sessionId);

        if (session == default)
            return Failure("Invalid session");

        if (!session.IsValid)
            return Failure("Session is not valid");

        identity.AddClaim(new Claim(AuthConstants.PlayerIdClaimType, session.PlayerId.ToString()));
        return Success();
    }
}