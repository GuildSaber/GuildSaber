using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.Api.Features.Guilds.Levels.Playlists.Http;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;
using GuildSaber.Mod.Features.GuildSaber.Runtime;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

public class PlaylistDownloader(GuildSaberClient client, GuildSaberManager manager, Logger logger)
{
    public bool IsDownloading { get; private set; }

    /// <summary>
    /// Event called when the whole download is completed
    /// </summary>
    public event Action<int, int>? EventPlaylistsDownloadCompleted;

    /// <summary>
    /// Event called for every unique playlists
    /// </summary>
    public event Action<string, string, bool>? EventUniquePlaylistDownloadCompleted;

    public Task DownloadPlaylistsAsync(CategoryId categoryId) => DownloadPlaylistsAsync(-1, int.MaxValue, categoryId);

    public Task DownloadPlaylistsAsync()
        => DownloadPlaylistsAsync(rangeMin: -1, rangeMax: int.MaxValue, categoryId: null);

    public async Task DownloadPlaylistsAsync(int rangeMin, int rangeMax, CategoryId? categoryId)
    {
        if (IsDownloading) throw new InvalidOperationException("Already downloading playlists");
        if (manager.State is not GuildSaberRuntimeState.Ready(var snapshot))
            throw new InvalidOperationException("GuildSaber is not ready to download playlists.");

        IsDownloading = true;

        try
        {
            var currentGuildExtended = snapshot.CurrentGuildExtended;
            var categories = currentGuildExtended.Categories;
            var levels = snapshot.LevelStats;
            var guildPlaylistsPath = GetGuildPlaylistsPath(currentGuildExtended.Guild);

            var (successfulPlaylists, failedPlaylists) = (0, 0);
            foreach (var category in categories.Where(c => !categoryId.HasValue || c.Id == categoryId.Value))
            {
                var playlistsPath = Path.Combine(guildPlaylistsPath,
                    PlaylistUtilities.SanitizeFileName(category.Info.Name));

                if (!Directory.Exists(playlistsPath))
                    Directory.CreateDirectory(playlistsPath);

                foreach (var level in levels.Where(x => x.Level.CategoryId == category.Id))
                {
                    if (level.Level.Order < rangeMin || level.Level.Order > rangeMax) continue;
                    if (categoryId.HasValue && level.Level.CategoryId != categoryId) continue;

                    var response = await client.Playlists
                        .GetByLevelIdAsync(level.Level.Id, PlaylistRequests.PlaylistFilter.None, null);

                    if (!response.TryGetValue(out var resultPlaylist, out var error) || resultPlaylist is null)
                    {
                        logger.Error(
                            $"[GuildSaber/PlaylistDownloader] Failed to download playlist for {level.Level.Info.Name}: {error}"
                        );

                        failedPlaylists += 1;
                        EventUniquePlaylistDownloadCompleted?.Invoke(category.Info.Name, level.Level.Info.Name, false);
                        continue;
                    }

                    var playlistFilename = PlaylistUtilities.GetPlaylistFileName(
                        level.Level,
                        currentGuildExtended
                    );

                    var path = Path.Combine(playlistsPath, playlistFilename);
                    if (File.Exists(path)) File.Delete(path);

                    await using var stream = File.Create(path);
                    await client.Playlists.WriteToStream(stream, resultPlaylist.Value);

                    successfulPlaylists += 1;
                    EventUniquePlaylistDownloadCompleted?.Invoke(category.Info.Name, level.Level.Info.Name, true);
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
        => Path.Combine(".", "Playlists", "GuildSaber", PlaylistUtilities.SanitizeFileName(guild.Info.Name));
}