using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Achievements;

/// <summary>
/// Client for interacting with achievement endpoints.
/// </summary>
public sealed class AchievementClient(
    HttpClient httpClient,
    Uri cdnBaseUri,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets an achievement by its ID.
    /// </summary>
    /// <param name="achievementId">The ID of the achievement to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the achievement if found, or null if not found.</returns>
    public async Task<Result<Achievement?>> GetByIdAsync(
        int achievementId, CancellationToken cancellationToken = default)
        => await httpClient.GetAsync($"achievements/{achievementId}", cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Achievement?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Achievement?>(
                        $"Failed to retrieve achievement with ID {achievementId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await TryAsync(() => response.Content
                            .ReadFromJsonAsync<Achievement?>(jsonOptions, cancellationToken: cancellationToken),
                        cancellationToken)
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets all achievements for a context, optionally filtered by category.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="categoryId">Optional category ID to filter by.</param>
    /// <param name="hasCategory">If false, returns only achievements with no category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing an array of achievements.</returns>
    public async Task<Result<Achievement[]>> GetByContextIdAsync(
        int contextId, int? categoryId, bool hasCategory, CancellationToken cancellationToken = default)
    {
        var url = $"contexts/{contextId}/achievements";
        if (categoryId.HasValue) url += $"?categoryId={categoryId.Value}&hasCategory={hasCategory}";
        else url += $"?hasCategory={hasCategory}";

        return await httpClient.GetAsync(url, cancellationToken)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Achievement[]>(
                        $"Failed to retrieve achievements for context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                            .ReadFromJsonAsync<Achievement[]>(jsonOptions, cancellationToken: cancellationToken),
                        cancellationToken)
                    .ConfigureAwait(false))!
            };
    }

    public Uri GetCoverUrl(int achievementId) => new(cdnBaseUri, $"achievements/{achievementId}/cover.png");
}