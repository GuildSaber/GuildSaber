namespace GuildSaber.Database.Models.Server.Songs;

public readonly record struct SongStats(
    float BPM,
    float Duration,
    bool IsAutoMapped
);