using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Members.LevelStats.LevelStatResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;

public sealed class LevelStatClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    public async Task<Result<MemberLevelStat[]>> GetByPlayerIdAsync(
        int contextId, int playerId, CancellationToken token)
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

    public async Task<Result<MemberLevelStat[]>> GetAtMeAsync(int contextId, CancellationToken token)
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