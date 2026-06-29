namespace GuildSaber.Api.Features.Scores.Http;

public static class ScoreResponses
{
    public record ScoreStatistics(
        WinTracker WinTracker,
        HitTracker HitTracker,
        AccuracyTracker AccuracyTracker,
        ScoreGraphTracker ScoreGraphTracker
    );

    public record WinTracker(
        bool IsWin,
        float EndTime,
        int PauseCount,
        float TotalPauseDuration,
        float JumpDistance,
        float AverageHeight,
        int TotalScore,
        int MaxScore,
        // ReSharper disable once MemberHidesStaticFromOuterClass
        AverageHeadPosition? AverageHeadPosition
    );

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

    public readonly record struct AverageHeadPosition(float X, float Y, float Z);
}