namespace GuildSaber.Api.Features.Guilds.Levels.Playlists;

public static class PlaylistResponses
{
    public record struct Playlist(
        string PlaylistTitle,
        string PlaylistAuthor,
        string PlaylistDescription,
        PlaylistCustomData? CustomData,
        PlaylistSong[] Songs,
        string? Image
    );

    public readonly record struct PlaylistCustomData(string? SyncURL);
    public readonly record struct PlaylistSong(string Hash, PlaylistDifficultyData[] Difficulties);
    public readonly record struct PlaylistDifficultyData(string Characteristic, string Name);
}