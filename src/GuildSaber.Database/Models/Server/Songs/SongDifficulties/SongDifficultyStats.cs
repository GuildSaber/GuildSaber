using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Songs.SongDifficulties;

public record SongDifficultyStats(
    MaxScore MaxScore,
    NJS NoteJumpSpeed,
    int NoteCount,
    int BombCount,
    int ObstacleCount,
    float NotesPerSecond,
    double Duration
);

public class SongDifficultyStatsConfiguration : IComplexPropertyConfiguration<SongDifficultyStats>
{
    public ComplexPropertyBuilder<SongDifficultyStats> Configure(
        ComplexPropertyBuilder<SongDifficultyStats> builder)
    {
        builder.Property(x => x.MaxScore)
            .HasConversion<int>(from => from, to => MaxScore.CreateUnsafe(to).Value);
        builder.Property(x => x.NoteJumpSpeed)
            .HasConversion<float>(from => from, to => NJS.CreateUnsafe(to).Value);

        return builder;
    }
}