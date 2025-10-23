using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed record BeatLeaderScore : AbstractScore
{
    public required BeatLeaderScoreId? BeatLeaderScoreId { get; init; }

    public required ScoreStatistics? ScoreStatistics { get; init; }
}

public class BeatLeaderScoreConfiguration : IEntityTypeConfiguration<BeatLeaderScore>
{
    public void Configure(EntityTypeBuilder<BeatLeaderScore> builder)
    {
        builder.HasBaseType<AbstractScore>();
        builder.ComplexProperty(x => x.ScoreStatistics).Configure(new ScoreStatisticConfiguration());
        builder.Property(x => x.BeatLeaderScoreId)
            .HasConversion<int?>(from => from, to => BeatLeaderScoreId.CreateUnsafe(to));
    }
}

public class ScoreStatistics
{
    public required WinTracker WinTracker { get; init; }
    public required HitTracker HitTracker { get; init; }
    public required AccuracyTracker AccuracyTracker { get; init; }
    public required ScoreGraphTracker ScoreGraphTracker { get; init; }
}

public class ScoreStatisticConfiguration : IComplexPropertyConfiguration<ScoreStatistics>
{
    public ComplexPropertyBuilder<ScoreStatistics> Configure(ComplexPropertyBuilder<ScoreStatistics> builder)
    {
        builder.ComplexProperty(x => x.WinTracker, x => x.ComplexProperty(y => y.AverageHeadPosition)
            .IsRequired()).IsRequired();
        builder.ComplexProperty(x => x.HitTracker).IsRequired();
        builder.ComplexProperty(x => x.AccuracyTracker).IsRequired();
        builder.ComplexProperty(x => x.ScoreGraphTracker).IsRequired();

        return builder;
    }
}

public readonly record struct AverageHeadPosition(float X, float Y, float Z);

public record WinTracker(
    bool IsWin,
    float EndTime,
    int PauseCount,
    float TotalPauseDuration,
    float JumpDistance,
    float AverageHeight,
    int TotalScore,
    int MaxScore
)
{
    public required AverageHeadPosition? AverageHeadPosition { get; init; }
}

public record HitTracker(
    int Max115Streak,
    float LeftTiming,
    float RightTiming,
    int LeftMiss,
    int RightMiss,
    int LeftBadCuts,
    int RightBadCuts,
    int LeftBombs,
    int RightBombs
);

public record AccuracyTracker(
    float AccRight,
    float AccLeft,
    float LeftPreswing,
    float RightPreswing,
    float LeftPostSwing,
    float RightPostSwing,
    float LeftTimeDependence,
    float RightTimeDependence,
    IReadOnlyList<float> LeftAverageCutGraphGrid,
    IReadOnlyList<float> RightAverageCutGrid,
    IReadOnlyList<float> AccuracyGrid
);

public record ScoreGraphTracker(List<float> Graph);