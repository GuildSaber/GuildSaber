using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using GuildSaber.Api.Features.Guilds.Levels.Playlists;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using Zenject;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class PlaylistDownloader(
    [Inject] Logger logger,
    [Inject] GuildSaberClient client,
    [Inject] GuildSaberConfig config,
    [Inject] GuildSaberCache cache)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Event called when the whole download is completed
    /// </summary>
    public event Action<int, int> EventPlaylistsDownloadCompleted = null!;

    /// <summary>
    /// Event called for every unique playlists
    /// </summary>
    public event Action<string, string, bool> EventUniquePlaylistDownloadCompleted = null!;

    //TODO: Make the playlist download something shared between the bot and the mod in GuildSaber.Common.
    public async void DownloadPlaylists()
    {
        var (guild, _, _, categories) = cache.GuildsExtended[config.GuildId];

        var guildName = guild.Info.Name;
        var levels = cache.MemberLevelStats[config.ContextId];

        var (successfulPlaylists, failedPlaylists) = (0, 0);
        foreach (var category in categories)
        {
            var playlistsPath = "./Playlists/GuildSaber/" + guildName + "/" + category.Info.Name + "/";
            if (!Directory.Exists(playlistsPath))
                Directory.CreateDirectory(playlistsPath);

            var levelCount = 0;
            foreach (var level in levels.Where(x => x.Level.CategoryId == category.Id))
            {
                var response = await client.Playlists
                    .GetByLevelIdAsync(level.Level.Id, PlaylistRequests.PlaylistFilter.None, null);

                if (!response.TryGetValue(out var resultPlaylist, out var error))
                {
                    logger.Error(
                        $"[GuildSaber/PlaylistDownloader] Failed to download playlist for {level.Level.Info.Name}: {error}"
                    );
                    failedPlaylists += 1;
                    EventUniquePlaylistDownloadCompleted.Invoke(category.Info.Name, level.Level.Info.Name, false);
                    levelCount += 1;
                    continue;
                }

                var serialized = JsonSerializer.Serialize(resultPlaylist, _jsonOptions);
                var playlistFilename = levelCount.ToString("000") + " " + level.Level.Info.Name + ".bplist";
                var path = playlistsPath + playlistFilename;

                if (File.Exists(path)) File.Delete(path);

                await File.WriteAllTextAsync(path, serialized);

                levelCount += 1;
                successfulPlaylists += 1;
                EventUniquePlaylistDownloadCompleted.Invoke(category.Info.Name, level.Level.Info.Name, true);
            }
        }

        EventPlaylistsDownloadCompleted.Invoke(successfulPlaylists, failedPlaylists);
    }
}