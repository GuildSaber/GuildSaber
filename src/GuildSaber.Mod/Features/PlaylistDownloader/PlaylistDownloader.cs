using System;
using System.IO;
using System.Linq;
using GuildSaber.Api.Features.Guilds.Levels.Playlists;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.PlaylistDownloader.Types;
using SongCore.OverrideClasses;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

public class PlaylistDownloader([Inject] Logger logger, [Inject] GuildSaberClient client, [Inject] GuildSaberConfig config, [Inject] GuildSaberCache cache)
{

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
        var selectedGuild = cache.GuildsExtended[config.PlayerCard.GuildId];

        string guildName = selectedGuild.Guild.Info.Name;
        var categories = selectedGuild.Categories;
        var levels = cache.MemberLevelStats[config.PlayerCard.ContextId];

        int successfulPlaylists = 0;
        int failedPlaylists = 0;
        
        foreach (var category in categories)
        {
            string playlistsPath = "./Playlists/GuildSaber/" + guildName + "/" + category.Info.Name + "/";
            if (!Directory.Exists(playlistsPath))
                Directory.CreateDirectory(playlistsPath);

            int levelCount = 0;
            foreach (var level in levels.Where(x => x.Level.CategoryId == category.Id))
            {
                var response =
                    await client.Playlists.GetByLevelIdAsync(level.Level.Id, PlaylistRequests.PlaylistFilter.None, null);
                
                if (!response.TryGetValue(out var resultPlaylist, out var error))
                {
                    logger.Error($"[GuildSaber/PlaylistDownloader] Failed to download playlist for {level.Level.Info.Name}: {error}");
                    failedPlaylists += 1;
                    EventUniquePlaylistDownloadCompleted.Invoke(category.Info.Name, level.Level.Info.Name, false);
                    levelCount += 1;
                    continue;
                }
                
                var serializable = new SerializablePlaylist(resultPlaylist!.Value);
                
                string serialized = Newtonsoft.Json.JsonConvert.SerializeObject(serializable);
                string playlistFilename = levelCount.ToString("000") + " " + level.Level.Info.Name + ".bplist";
                string path = playlistsPath + playlistFilename;
                
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