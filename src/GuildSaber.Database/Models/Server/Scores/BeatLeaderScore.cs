using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed class BeatLeaderScore : AbstractScore
{
    public required BeatLeaderScoreId? BeatLeaderScoreId { get; init; }
    public required ScoreStatistics? Statistics { get; init; }
}

public class BeatLeaderScoreConfiguration : IEntityTypeConfiguration<BeatLeaderScore>
{
    public void Configure(EntityTypeBuilder<BeatLeaderScore> builder)
    {
        builder.HasBaseType<AbstractScore>();
        builder.ComplexProperty(x => x.Statistics).Configure(new ScoreStatisticConfiguration());
        builder.Property(x => x.BeatLeaderScoreId)
            .HasConversion<int?>(from => from, to => BeatLeaderScoreId.CreateUnsafe(to));
    }
}

public class ScoreStatistics
{
    public WinTracker WinTracker { get; set; } = null!;
    public HitTracker HitTracker { get; set; } = null!;
    public AccuracyTracker AccuracyTracker { get; set; } = null!;
    public ScoreGraphTracker ScoreGraphTracker { get; set; } = null!;
}

public class ScoreStatisticConfiguration : IComplexPropertyConfiguration<ScoreStatistics>
{
    public ComplexPropertyBuilder<ScoreStatistics> Configure(ComplexPropertyBuilder<ScoreStatistics> builder)
    {
        builder.HasDiscriminator();
        builder.ComplexProperty(x => x.WinTracker)
            .ComplexProperty(x => x.AverageHeadPosition);
        builder.ComplexProperty(x => x.HitTracker);
        builder.ComplexProperty(x => x.AccuracyTracker);
        builder.ComplexProperty(x => x.ScoreGraphTracker);

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
    float LeftPreSwing,
    float RightPreSwing,
    float LeftPostSwing,
    float RightPostSwing,
    float LeftTimeDependence,
    float RightTimeDependence,
    IReadOnlyList<float> LeftAverageCutGraphGrid,
    IReadOnlyList<float> RightAverageCutGraphGrid,
    IReadOnlyList<float> AccuracyGrid
);

public record ScoreGraphTracker(List<float> Graph);