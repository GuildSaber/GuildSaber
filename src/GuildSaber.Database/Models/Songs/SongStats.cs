namespace GuildSaber.Database.Models.Songs;

public readonly record struct SongStats(
    float BPM,
    float Duration,
    bool IsAutoMapped
);