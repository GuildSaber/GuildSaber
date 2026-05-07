using System.Collections.Generic;
using GuildSaber.Api.Features.Guilds.Levels.Playlists;

namespace GuildSaber.Mod.Features.PlaylistDownloader.Types;

public class SerializablePlaylistCustomData
{
    public bool AllowDuplicates { get; set; } = false;
}


public class SerializablePlaylistSong
{
    public string levelName { get; set; } = string.Empty;
    public string levelAuthorName { get; set; } = string.Empty;
    public string hash { get; set; } = string.Empty;
    public string levelid { get; set; } = string.Empty;
    public List<SerializablePlaylistDifficulty> difficulties = new List<SerializablePlaylistDifficulty>();
}

public class SerializablePlaylistDifficulty
{
    public string characteristic { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
}

public class SerializablePlaylist
{
    public SerializablePlaylist(PlaylistResponses.Playlist guildSaberPlaylist)
    {
        playlistTitle = guildSaberPlaylist.PlaylistTitle;
        playlistAuthor = guildSaberPlaylist.PlaylistAuthor;
        image = guildSaberPlaylist.Image ?? string.Empty;

        foreach (var song in guildSaberPlaylist.Songs)
        {
            var serializablePlaylistSong = new SerializablePlaylistSong();
            
            serializablePlaylistSong.hash = song.Hash;
            foreach (var difficulty in song.Difficulties)
            {
                var newDiff = new SerializablePlaylistDifficulty();
                newDiff.characteristic = difficulty.Characteristic;
                newDiff.name = difficulty.Name;

                serializablePlaylistSong.difficulties.Add(newDiff);
            }
            
            songs.Add(serializablePlaylistSong);
        }
    }
    
    public string playlistTitle { get; set; } = string.Empty;
    public string playlistAuthor { get; set; } = string.Empty;
    public List<SerializablePlaylistSong> songs = new List<SerializablePlaylistSong>();
    public string image { get; set; } = string.Empty;
}