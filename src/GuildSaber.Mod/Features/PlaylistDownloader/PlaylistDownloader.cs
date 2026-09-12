using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GuildSaber.Api.Features.Guilds.Achievements.Playlists.Http;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Guilds.Achievements.Playlists;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

public class PlaylistDownloader(GuildSaberClient client, GuildSaberManager manager, Logger logger)
{
    private const string CoverFileName = "cover.png";

    public bool IsDownloading { get; private set; }

    /// <summary>
    /// Event called when the whole download is completed
    /// </summary>
    public event Action<int, int>? EventPlaylistsDownloadCompleted;

    /// <summary>
    /// Event called for every unique playlists
    /// </summary>
    public event Action<string, string, bool>? EventUniquePlaylistDownloadCompleted;

    public Task DownloadPlaylistsAsync(CategoryId categoryId)
        => DownloadPlaylistsAsync(-1, int.MaxValue, categoryId, includeUnordered: true);

    public Task DownloadPlaylistsAsync()
        => DownloadPlaylistsAsync(rangeMin: -1, rangeMax: int.MaxValue, categoryId: null, includeUnordered: true);

    public async Task DownloadPlaylistsAsync(int rangeMin, int rangeMax, CategoryId? categoryId)
        => await DownloadPlaylistsAsync(rangeMin, rangeMax, categoryId, includeUnordered: false);

    private async Task DownloadPlaylistsAsync(
        int rangeMin, int rangeMax, CategoryId? categoryId, bool includeUnordered)
    {
        if (IsDownloading) throw new InvalidOperationException("Already downloading playlists");
        if (manager.State is not GuildSaberRuntimeState.Ready(var snapshot))
            throw new InvalidOperationException("GuildSaber is not ready to download playlists.");

        IsDownloading = true;

        try
        {
            var currentGuildExtended = snapshot.CurrentGuildExtended;
            var categories = currentGuildExtended.Categories;
            var achievements = snapshot.AchievementStats;
            var guildPlaylistsPath = GetGuildPlaylistsPath(currentGuildExtended.Guild);

            Directory.CreateDirectory(guildPlaylistsPath);
            await DownloadCoverAsync(
                client.Guilds.GetLogoUrl(currentGuildExtended.Guild.Id),
                guildPlaylistsPath
            );

            var (successfulPlaylists, failedPlaylists) = (0, 0);
            foreach (var category in categories.Where(c => !categoryId.HasValue || c.Id == categoryId.Value))
            {
                var playlistsPath = Path.Combine(guildPlaylistsPath,
                    PlaylistUtilities.SanitizeFileName(category.Info.Name));

                Directory.CreateDirectory(playlistsPath);
                await DownloadCoverAsync(client.Categories.GetLogoUrl(category.Id), playlistsPath);

                foreach (var achievementStat in achievements.Where(x => x.Achievement.CategoryId == category.Id))
                {
                    if (achievementStat.Achievement.Progression is AchievementProgression.Ordered progression)
                    {
                        if (progression.Order < rangeMin || progression.Order > rangeMax) continue;
                    }
                    else if (!includeUnordered)
                    {
                        continue;
                    }

                    if (categoryId.HasValue && achievementStat.Achievement.CategoryId != categoryId) continue;

                    var response = await client.Playlists
                        .GetByAchievementIdAsync(
                            achievementStat.Achievement.Id, PlaylistRequests.PlaylistFilter.None, null);

                    if (!response.TryGetValue(out var resultPlaylist, out var error) || resultPlaylist is null)
                    {
                        logger.Error(
                            $"[GuildSaber/PlaylistDownloader] Failed to download playlist for {achievementStat.Achievement.Info.Name}: {error}"
                        );

                        failedPlaylists += 1;
                        EventUniquePlaylistDownloadCompleted?.Invoke(
                            category.Info.Name, achievementStat.Achievement.Info.Name, false);
                        continue;
                    }

                    var playlistFilename = PlaylistUtilities.GetPlaylistFileName(
                        achievementStat.Achievement,
                        currentGuildExtended
                    );

                    var path = Path.Combine(playlistsPath, playlistFilename);
                    if (File.Exists(path)) File.Delete(path);

                    await using var stream = File.Create(path);
                    await client.Playlists.WriteToStream(stream, resultPlaylist.Value);

                    successfulPlaylists += 1;
                    EventUniquePlaylistDownloadCompleted?.Invoke(
                        category.Info.Name, achievementStat.Achievement.Info.Name, true);
                }
            }

            IsDownloading = false;
            EventPlaylistsDownloadCompleted?.Invoke(successfulPlaylists, failedPlaylists);
        }
        finally
        {
            IsDownloading = false;
        }
    }

    public string GetGuildPlaylistsPath(GuildResponses.Guild guild)
        => Path.Combine(".", "Playlists", PlaylistUtilities.SanitizeFileName(guild.Info.Name));

    private async Task DownloadCoverAsync(Uri coverUri, string directoryPath)
    {
        try
        {
            using var response = await client.HttpClient.GetAsync(coverUri);
            if (response.StatusCode == HttpStatusCode.NotFound) return;

            if (!response.IsSuccessStatusCode)
            {
                logger.Warn(
                    $"[GuildSaber/PlaylistDownloader] Failed to download cover from {coverUri}: " +
                    $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"
                );
                return;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!texture.LoadImage(imageBytes, false))
                {
                    logger.Warn(
                        $"[GuildSaber/PlaylistDownloader] Failed to decode cover from {coverUri}"
                    );
                    return;
                }

                var pngBytes = texture.EncodeToPNG();
                if (pngBytes is null || pngBytes.Length == 0)
                {
                    logger.Warn(
                        $"[GuildSaber/PlaylistDownloader] Failed to encode cover from {coverUri} as PNG"
                    );
                    return;
                }

                var coverPath = Path.Combine(directoryPath, CoverFileName);
                await using var stream = File.Create(coverPath);
                await stream.WriteAsync(pngBytes, 0, pngBytes.Length);
            }
            finally
            {
                Object.Destroy(texture);
            }
        }
        catch (Exception exception)
        {
            logger.Warn(
                $"[GuildSaber/PlaylistDownloader] Failed to save cover from {coverUri}: {exception}"
            );
        }
    }
}