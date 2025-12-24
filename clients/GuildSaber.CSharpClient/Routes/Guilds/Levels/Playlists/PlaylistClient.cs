using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.PlaylistResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;

/// <summary>
/// Client for interacting with playlist endpoints.
/// </summary>
public sealed class PlaylistClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets the playlist for a ranked map list level by its ID.
    /// </summary>
    /// <param name="levelId">The ID of the level to retrieve the playlist for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the playlist if found, or null if not found.</returns>
    public async Task<Result<Playlist?>> GetByLevelIdAsync(int levelId, CancellationToken cancellationToken = default)
        => await httpClient.GetAsync($"levels/{levelId}/playlist", cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Playlist?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Playlist?>(
                        $"Failed to retrieve playlist for level with ID {levelId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<Playlist?>(jsonOptions, cancellationToken: cancellationToken))
                    .ConfigureAwait(false)
            };
}