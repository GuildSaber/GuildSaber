namespace GuildSaber.Database.Models.Server.Songs;

public readonly record struct SongInfo(
    string BeatSaverName,
    string SongName,
    string SongSubName,
    string SongAuthorName,
    string MapperName
);