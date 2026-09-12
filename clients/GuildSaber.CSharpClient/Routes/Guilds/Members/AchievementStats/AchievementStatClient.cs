using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http.AchievementStatResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.AchievementStats;

/// <summary>
/// Client for interacting with member achievement stat endpoints.
/// </summary>
public sealed class AchievementStatClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets achievement stats for a specific player in a specific context.
    /// </summary>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing an array of member achievement stats.</returns>
    public async Task<Result<MemberAchievementStat[]>> GetByPlayerIdAsync(
        int playerId, int contextId, CancellationToken token = default)
        => await httpClient.GetAsync($"contexts/{contextId}/members/{playerId}/achievement-stats", token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberAchievementStat[]>(
                        $"Failed to retrieve achievement stats for player {playerId} in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<MemberAchievementStat[]>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };

    /// <summary>
    /// Gets achievement stats for the currently authenticated player in a specific context.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing an array of member achievement stats, or failure if unauthorized or not found.</returns>
    public async Task<Result<MemberAchievementStat[]>> GetAtMeAsync(int contextId, CancellationToken token = default)
        => await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, $"contexts/{contextId}/members/@me/achievement-stats")
                    {
                        Headers = { Authorization = authenticationHeader }
                    }, token)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.NotFound }
                    => Failure<MemberAchievementStat[]>(
                        "Failed to retrieve achievement stats: Unauthorized or not found"),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberAchievementStat[]>(
                        $"Failed to retrieve achievement stats for current player in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<MemberAchievementStat[]>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };
}