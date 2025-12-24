using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Levels.LevelResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Levels;

/// <summary>
/// Client for interacting with level endpoints.
/// </summary>
public sealed class LevelClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets a level by its ID.
    /// </summary>
    /// <param name="levelId">The ID of the level to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the level if found, or null if not found.</returns>
    public async Task<Result<Level?>> GetByIdAsync(int levelId, CancellationToken cancellationToken = default)
        => await httpClient.GetAsync($"levels/{levelId}", cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Level?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Level?>(
                        $"Failed to retrieve level with ID {levelId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<Level?>(jsonOptions, cancellationToken: cancellationToken))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets all levels for a context, optionally filtered by category.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="categoryId">Optional category ID to filter by.</param>
    /// <param name="hasCategory">If false, returns only levels with no category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing an array of levels.</returns>
    public async Task<Result<Level[]>> GetByContextIdAsync(
        int contextId, int? categoryId = null, bool? hasCategory = null, CancellationToken cancellationToken = default)
    {
        var url = $"context/{contextId}/levels";
        if (categoryId.HasValue)
            url += $"?categoryId={categoryId.Value}";
        else if (hasCategory.HasValue)
            url += $"?hasCategory={hasCategory.Value}";

        return await httpClient.GetAsync(url, cancellationToken)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Level[]>(
                        $"Failed to retrieve levels for context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await Try(() => response.Content
                        .ReadFromJsonAsync<Level[]>(jsonOptions, cancellationToken: cancellationToken))
                    .ConfigureAwait(false))!
            };
    }
}