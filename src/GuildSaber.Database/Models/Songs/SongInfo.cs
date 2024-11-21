namespace GuildSaber.Database.Models.Songs;

public readonly record struct SongInfo(
    string BeatSaverName,
    string SongName,
    string SongSubName,
    string SongAuthorName,
    string MapperName
);