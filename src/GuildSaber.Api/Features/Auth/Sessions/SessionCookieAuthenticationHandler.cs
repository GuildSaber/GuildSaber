using System.Text.Encodings.Web;
using GuildSaber.Api.Features.Auth.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GuildSaber.Api.Features.Auth.Sessions;

public sealed class SessionCookieAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SessionCookieService cookieService,
    SessionTokenService tokenService,
    SessionValidator sessionValidator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(cookieService.CookieName, out var token) ||
            string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.NoResult();

        try
        {
            var principal = tokenService.ValidateToken(token);
            var sessionId = principal.GetSessionId();
            if (sessionId is null)
                return AuthenticateResult.Fail("Session ID not found in session cookie.");

            var validationResult = await sessionValidator.ValidateAndApplySessionAsync(sessionId.Value, principal);
            return validationResult.TryGetError(out var error)
                ? AuthenticateResult.Fail(error)
                : AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (SecurityTokenException)
        {
            return AuthenticateResult.Fail("Invalid session cookie.");
        }
        catch (ArgumentException)
        {
            return AuthenticateResult.Fail("Invalid session cookie.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}