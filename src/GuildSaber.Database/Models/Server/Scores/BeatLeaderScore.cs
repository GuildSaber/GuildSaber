using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Scores;

public sealed record BeatLeaderScore : AbstractScore
{
    public required uint? BeatLeaderScoreId { get; init; }

    /// <remarks>
    /// This is a workaround for EF Core not supporting nullable complex types.
    /// We use a separate boolean to track if the ScoreStatistics is set or not (<see cref="HasScoreStatistics" />).
    /// If HasScoreStatistics is false, ScoreStatistics will be null.
    /// If HasScoreStatistics is true, ScoreStatistics will be the value of ScoreStatisticsRepresentation.
    /// This way, we can still have a nullable complex type. (It's equivalent to storing a tagged union in a database)
    /// The other solution would be making all fields in ScoreStatistics nullable in the representation, and make a property
    /// that checks if all fields are null to determine if the whole struct is null, but that sounds semantically wrong.
    /// </remarks>
    private ScoreStatistics ScoreStatisticsRepresentation { get; init; }

    public bool HasScoreStatistics { get; private init; }
    public static string ScoreStatisticsRepresentationName => nameof(ScoreStatisticsRepresentation);

    public required ScoreStatistics? ScoreStatistics
    {
        get => HasScoreStatistics ? ScoreStatisticsRepresentation : null;
        init
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
    public required WinTracker WinTracker { get; init; }
    public required HitTracker HitTracker { get; init; }
    public required AccuracyTracker AccuracyTracker { get; init; }
    public required ScoreGraphTracker ScoreGraphTracker { get; init; }
}

public class ScoreStatisticConfiguration : IComplexPropertyConfiguration<ScoreStatistics>
{
    public ComplexPropertyBuilder<ScoreStatistics> Configure(ComplexPropertyBuilder<ScoreStatistics> builder)
    {
        builder.ComplexProperty(x => x.WinTracker, x => x
            .Ignore(y => y.AverageHeadPosition)
            .ComplexProperty<AverageHeadPosition>(WinTracker.AverageHeadPositionRepresentationName));
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
    private AverageHeadPosition AverageHeadPositionRepresentation { get; init; }
    public bool HasAverageHeadPosition { get; private init; }
    public static string AverageHeadPositionRepresentationName => nameof(AverageHeadPositionRepresentation);

    public required AverageHeadPosition? AverageHeadPosition
    {
        get => HasAverageHeadPosition ? AverageHeadPositionRepresentation : null;
        init
        {
            HasAverageHeadPosition = value.HasValue;
            AverageHeadPositionRepresentation = value.GetValueOrDefault();
        }
    }
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

public readonly record struct ScoreGraphTracker(List<float> Graph);