using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Levels.Http;
using GuildSaber.Common.Result;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.Http.PlaylistRequests;
using static GuildSaber.Api.Features.Guilds.Levels.Playlists.Http.PlaylistResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;

/// <summary>
/// Client for interacting with playlist endpoints.
/// </summary>
public sealed class PlaylistClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    public JsonSerializerOptions JsonOptions => jsonOptions;

    public readonly record struct LevelWithPlaylist(LevelResponses.Level Level, Playlist? Playlist);

    /// <summary>
    /// Gets the playlist for a ranked map list level by its ID.
    /// </summary>
    /// <param name="levelId">The ID of the level to retrieve the playlist for.</param>
    /// <param name="filter">The filter to apply to the playlist.</param>
    /// <param name="playerId">Optional player ID for personalized playlist data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the playlist if found, or null if not found.</returns>
    public async Task<Result<Playlist?>> GetByLevelIdAsync(
        int levelId, PlaylistFilter filter, PlayerId? playerId, CancellationToken cancellationToken = default)
        => await httpClient
                .GetAsync(
                    $"levels/{levelId}/playlist?filter={filter}{(playerId is null ? "" : $"&playerId={playerId}")}",
                    cancellationToken)
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

    public async Task<Result<IEnumerable<LevelWithPlaylist>>> GetAsync(
        LevelResponses.Level[] levels, PlaylistFilter filter, PlayerId? playerId,
        CancellationToken cancellationToken = default)
        => (await Task.WhenAll(levels.Select(async level =>
                    await GetByLevelIdAsync(level.Id, filter, playerId, cancellationToken)
                        .Map(static (playlist, level) => new LevelWithPlaylist(level, playlist), level)))
                .ConfigureAwait(false))
            .Reduce();
}