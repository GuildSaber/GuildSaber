using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Common.Result;
using static GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http.PlaylistRequests;
using static GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http.PlaylistResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Achievements.Playlists;

/// <summary>
/// Client for interacting with playlist endpoints.
/// </summary>
public sealed class PlaylistClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    public JsonSerializerOptions JsonOptions => jsonOptions;

    public readonly record struct AchievementWithPlaylist(
        AchievementResponses.Achievement Achievement,
        Playlist? Playlist);

    /// <summary>
    /// Gets the playlist for an achievement by its ID.
    /// </summary>
    /// <param name="achievementId">The ID of the achievement to retrieve the playlist for.</param>
    /// <param name="filter">The filter to apply to the playlist.</param>
    /// <param name="playerId">Optional player ID for personalized playlist data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the playlist if found, or null if not found.</returns>
    public async Task<Result<Playlist?>> GetByAchievementIdAsync(
        int achievementId, PlaylistFilter filter, PlayerId? playerId, CancellationToken cancellationToken = default)
        => await httpClient
                .GetAsync(
                    $"achievements/{achievementId}/playlist?filter={filter}{(playerId is null ? "" : $"&playerId={playerId}")}",
                    cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Playlist?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Playlist?>(
                        $"Failed to retrieve playlist for achievement with ID {achievementId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await TryAsync(() => response.Content
                            .ReadFromJsonAsync<Playlist?>(jsonOptions, cancellationToken: cancellationToken),
                        cancellationToken)
                    .ConfigureAwait(false)
            };

    public async Task<Result<IEnumerable<AchievementWithPlaylist>>> GetAsync(
        AchievementResponses.Achievement[] achievements, PlaylistFilter filter, PlayerId? playerId,
        CancellationToken cancellationToken = default)
        => (await Task.WhenAll(achievements.Select(async achievement =>
                    await GetByAchievementIdAsync(achievement.Id, filter, playerId, cancellationToken)
                        .Map(static (playlist, achievement) => new AchievementWithPlaylist(achievement, playlist),
                            achievement)))
                .ConfigureAwait(false))
            .Reduce();
}