using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed record BeatLeaderScore : AbstractScore
{
    public bool HasScoreStatistics { get; private set; }

    private ScoreStatistics ScoreStatisticsRepresentation { get; set; }
    public static string ScoreStatisticsRepresentationName => nameof(ScoreStatisticsRepresentation);

    public ScoreStatistics? ScoreStatistics
    {
        get => HasScoreStatistics ? ScoreStatisticsRepresentation : null;
        set
        {
            HasScoreStatistics = value.HasValue;
            ScoreStatisticsRepresentation = value.GetValueOrDefault();
        }
    }
}

public class BeatLeaderScoreConfiguration : IEntityTypeConfiguration<BeatLeaderScore>
{
    public void Configure(EntityTypeBuilder<BeatLeaderScore> builder)
    {
        builder.HasBaseType<AbstractScore>();
        builder.Ignore(x => x.ScoreStatistics)
            .ComplexProperty<ScoreStatistics>(BeatLeaderScore.ScoreStatisticsRepresentationName)
            .Configure(new ScoreStatisticConfiguration());
    }
}

public readonly record struct ScoreStatistics
{
    public WinTracker WinTracker { get; init; }
    public HitTracker HitTracker { get; init; }
    public AccuracyTracker AccuracyTracker { get; init; }
    public ScoreGraphTracker ScoreGraphTracker { get; init; }
}

public class ScoreStatisticConfiguration : IComplexPropertyConfiguration<ScoreStatistics>
{
    public ComplexPropertyBuilder<ScoreStatistics> Configure(ComplexPropertyBuilder<ScoreStatistics> builder)
    {
        builder.ComplexProperty(x => x.WinTracker, x =>
            x.ComplexProperty(y => y.AveragePosition)
        );
        builder.ComplexProperty(x => x.HitTracker);
        builder.ComplexProperty(x => x.AccuracyTracker);
        builder.ComplexProperty(x => x.ScoreGraphTracker);

        return builder;
    }
}

public readonly record struct AveragePosition(float X, float Y, float Z);

public readonly record struct WinTracker(
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
    public required AveragePosition AveragePosition { get; init; }
}

public readonly record struct HitTracker(
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

public readonly record struct AccuracyTracker(
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

public readonly record struct ScoreGraphTracker(List<float> Graph);