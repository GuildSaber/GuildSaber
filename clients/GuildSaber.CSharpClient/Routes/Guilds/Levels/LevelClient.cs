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
}