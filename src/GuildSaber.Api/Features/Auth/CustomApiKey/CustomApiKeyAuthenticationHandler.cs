using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.CustomApiKey.ValidationTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GuildSaber.Api.Features.Auth.CustomApiKey;

public class CustomApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ICustomApiKeyAuthenticationService authenticationService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    [ExcludeFromCodeCoverage]
    private static AuthenticateResult? TryParseAuthenticationHeader(
        HttpRequest request, out AuthenticationHeaderValue? authHeaderValue)
    {
        authHeaderValue = null;

        if (!request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader) ||
            string.IsNullOrWhiteSpace(authHeader))
            return AuthenticateResult.NoResult();

        if (!AuthenticationHeaderValue.TryParse(authHeader, out authHeaderValue!))
            return AuthenticateResult.Fail("Invalid Authorization header.");

        return authHeaderValue.Scheme != BasicAuthenticationDefaults.AuthenticationScheme
            ? AuthenticateResult.NoResult()
            : null;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (TryParseAuthenticationHeader(Request, out var authHeaderValue) is { } result)
            return Task.FromResult(result);

        return !BasicCredential.TryParse(authHeaderValue?.Parameter, out var credential)
            ? Task.FromResult(AuthenticateResult.Fail("Error decoding credentials from header value."))
            : authenticationService.AuthenticateAsync(credential, Request.HttpContext.Connection.RemoteIpAddress);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = BasicAuthenticationDefaults.AuthenticationScheme;
        return base.HandleChallengeAsync(properties);
    }
}