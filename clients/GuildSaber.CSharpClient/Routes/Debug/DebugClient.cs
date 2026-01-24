using System.Net;
using System.Net.Http.Headers;
using CSharpFunctionalExtensions;

namespace GuildSaber.CSharpClient.Routes.Debug;

/// <summary>
/// Client for interacting with debug endpoints.
/// </summary>
public sealed class DebugClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader)
{
    /// <summary>
    /// Imports ranked score admin configuration states for the current user.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result indicating success (true) or not found (false).</returns>
    public async Task<Result<bool>> ImportAdminConfStatesAtMeAsync(CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/debug/import-admin-conf-states")
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.OK } => Success(true),
                { StatusCode: HttpStatusCode.NotFound } => Success(false),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<bool>(
                        $"Failed to import admin conf states at me: {(int)statusCode} ({reasonPhrase})"),
                _ => Success(true)
            };
}