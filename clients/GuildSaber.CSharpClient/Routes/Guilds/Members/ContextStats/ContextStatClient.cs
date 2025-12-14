using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Members.ContextStats.ContextStatResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Members.ContextStats;

public sealed class ContextStatClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    public async Task<Result<MemberContextStat?>> GetMemberContextStatsAsync(
        int contextId, int playerId, CancellationToken token)
        => await httpClient.GetAsync($"contexts/{contextId}/members/{playerId}/context-stats", token)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<MemberContextStat?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberContextStat?>(
                        $"Failed to retrieve context stats for player {playerId} in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<MemberContextStat?>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    public async Task<Result<MemberContextStat?>> GetAtMeAsync(int contextId, CancellationToken token)
        => await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, $"contexts/{contextId}/members/@me/context-stats")
                    {
                        Headers = { Authorization = authenticationHeader }
                    }, token)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.NotFound }
                    => Failure<MemberContextStat?>("Failed to retrieve context stats: Unauthorized or not found"),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<MemberContextStat?>(
                        $"Failed to retrieve context stats for current player in context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<MemberContextStat?>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };
}