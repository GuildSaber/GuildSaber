using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.LevelStatResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;

/// <summary>
/// Client for interacting with member level stat endpoints.
/// </summary>
public sealed class LevelStatClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets level stats for a specific player in a specific context.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing an array of member level stats.</returns>
    public async Task<Result<MemberLevelStat[]>> GetByPlayerIdAsync(
        int contextId, int playerId, CancellationToken token = default)
        => await httpClient.GetAsync($"contexts/{contextId}/members/{playerId}/level-stats", token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberLevelStat[]>(
                        $"Failed to retrieve level stats for player {playerId} in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await Try(() => response.Content
                        .ReadFromJsonAsync<MemberLevelStat[]>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false))!
            };

    /// <summary>
    /// Gets level stats for the currently authenticated player in a specific context.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing an array of member level stats, or failure if unauthorized or not found.</returns>
    public async Task<Result<MemberLevelStat[]>> GetAtMeAsync(int contextId, CancellationToken token = default)
        => await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, $"contexts/{contextId}/members/@me/level-stats")
                    {
                        Headers = { Authorization = authenticationHeader }
                    }, token)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.NotFound }
                    => Failure<MemberLevelStat[]>("Failed to retrieve level stats: Unauthorized or not found"),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberLevelStat[]>(
                        $"Failed to retrieve level stats for current player in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await Try(() => response.Content
                        .ReadFromJsonAsync<MemberLevelStat[]>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false))!
            };
}