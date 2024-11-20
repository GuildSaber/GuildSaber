namespace GuildSaber.Database.Models.Song;

public readonly record struct SongStats(
    float BPM,
    float Duration,
    bool IsAutoMapped
);