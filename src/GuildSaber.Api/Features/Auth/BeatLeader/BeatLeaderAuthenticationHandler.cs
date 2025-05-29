/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace AspNet.Security.OAuth.BeatLeader;

public partial class BeatLeaderAuthenticationHandler(
    IOptionsMonitor<BeatLeaderAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : OAuthHandler<BeatLeaderAuthenticationOptions>(options, logger, encoder)
{
    private static partial class Log
    {
        private const string UserProfileErrorMessage =
            "An error occurred while retrieving the user profile: the remote server returned a {Status} response with the following payload: {Headers} {Body}.";

        internal static async Task UserProfileErrorAsync(
            ILogger logger,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
            => UserProfileError(logger, response.StatusCode, response.Headers.ToString(),
                await response.Content.ReadAsStringAsync(cancellationToken));

        [LoggerMessage(1, LogLevel.Error, UserProfileErrorMessage)]
        private static partial void UserProfileError(
            ILogger logger,
            HttpStatusCode status,
            string headers,
            string body);
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        ClaimsIdentity identity,
        AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        var endpoint = Options.UserInformationEndpoint;

        if (Options.Fields.Count > 0)
            endpoint = QueryHelpers.AddQueryString(endpoint, "fields[user]", string.Join(',', Options.Fields));

        if (Options.Includes.Count > 0)
            endpoint = QueryHelpers.AddQueryString(endpoint, "include", string.Join(',', Options.Includes));

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        using var response = await Backchannel.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, Context.RequestAborted
        );
        if (!response.IsSuccessStatusCode)
        {
            await Log.UserProfileErrorAsync(Logger, response, Context.RequestAborted);
            throw new HttpRequestException("An error occurred while retrieving the user profile.");
        }

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));

        var principal = new ClaimsPrincipal(identity);
        var context = new OAuthCreatingTicketContext(
            principal, properties,
            Context, Scheme, Options, Backchannel,
            tokens, payload.RootElement
        );
        context.RunClaimActions(payload.RootElement);

        await Events.CreatingTicket(context);
        return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
    }
}