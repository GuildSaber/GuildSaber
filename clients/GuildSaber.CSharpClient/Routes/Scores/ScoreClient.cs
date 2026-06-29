using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Scores.Http.ScoreResponses;

namespace GuildSaber.CSharpClient.Routes.Scores;

/// <summary>
/// Client for interacting with score endpoints.
/// </summary>
public sealed class ScoreClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets score statistics for a BeatLeader score.
    /// </summary>
    /// <param name="scoreId">The score identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the score statistics if found, or null if not found.</returns>
    public async Task<Result<ScoreStatistics?>> GetStatisticsAsync(
        ScoreId scoreId,
        CancellationToken token = default)
        => await httpClient.GetAsync($"scores/{scoreId}/statistics", token)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<ScoreStatistics?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<ScoreStatistics?>(
                        $"Failed to retrieve score statistics for score {scoreId}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<ScoreStatistics>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };
}