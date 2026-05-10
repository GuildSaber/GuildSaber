using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GuildSaber.Api.Features.Guilds.Levels.Playlists;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Guilds.Levels.Playlists;
using GuildSaber.Mod.Features.GuildSaber;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class PlaylistDownloader(
    [Inject] Logger logger,
    [Inject] GuildSaberClient client,
    [Inject] GuildSaberConfig config,
    [Inject] GuildSaberCache cache)
{
    public bool IsDownloading { get; private set; }

    /// <summary>
    /// Event called when the whole download is completed
    /// </summary>
    public event Action<int, int> EventPlaylistsDownloadCompleted = null!;

    /// <summary>
    /// Event called for every unique playlists
    /// </summary>
    public event Action<string, string, bool> EventUniquePlaylistDownloadCompleted = null!;

    public async void DownloadPlaylists()
    {
        if (IsDownloading) throw new InvalidOperationException("Already downloading playlists");

        IsDownloading = true;
        var (guild, _, _, categories) = cache.GuildsExtended[config.GuildId];

        var guildName = guild.Info.Name;
        var levels = cache.MemberLevelStats[config.ContextId];

        var (successfulPlaylists, failedPlaylists) = (0, 0);
        foreach (var category in categories)
        {
            var playlistsPath = "./Playlists/GuildSaber/" + guildName + "/" + category.Info.Name + "/";
            if (!Directory.Exists(playlistsPath))
                Directory.CreateDirectory(playlistsPath);

            foreach (var level in levels.Where(x => x.Level.CategoryId == category.Id))
            {
                var response = await client.Playlists
                    .GetByLevelIdAsync(level.Level.Id, PlaylistRequests.PlaylistFilter.None, null);

                if (!response.TryGetValue(out var resultPlaylist, out var error) || resultPlaylist is null)
                {
                    logger.Error(
                        $"[GuildSaber/PlaylistDownloader] Failed to download playlist for {level.Level.Info.Name}: {error}"
                    );

                    failedPlaylists += 1;
                    EventUniquePlaylistDownloadCompleted.Invoke(category.Info.Name, level.Level.Info.Name, false);
                    continue;
                }

                var playlistFilename = PlaylistUtilities.GetPlaylistFileName(
                    level.Level,
                    cache.GuildsExtended[config.GuildId]
                );

                var path = Path.Combine(playlistsPath, playlistFilename);
                if (File.Exists(path)) File.Delete(path);

                await using var stream = File.Create(path);
                await client.Playlists.WriteToStream(stream, resultPlaylist.Value);

                successfulPlaylists += 1;
                EventUniquePlaylistDownloadCompleted.Invoke(category.Info.Name, level.Level.Info.Name, true);
            }
        }

        IsDownloading = false;
        EventPlaylistsDownloadCompleted.Invoke(successfulPlaylists, failedPlaylists);
    }
}