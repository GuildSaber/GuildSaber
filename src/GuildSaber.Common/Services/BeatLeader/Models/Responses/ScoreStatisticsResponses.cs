namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

/* Non-Exhaustive properties from BeatLeader (as some are redundant) */

public class ScoreStatistics
{
    public required HitTracker HitTracker { get; init; }
    public required AccuracyTracker AccuracyTracker { get; init; }
    public required WinTracker WinTracker { get; init; }
    public required ScoreGraphTracker ScoreGraphTracker { get; init; }
}

public class HitTracker
{
    public required int MaxStreak { get; init; }
    public required float LeftTiming { get; init; }
    public required float RightTiming { get; init; }
    public required int LeftMiss { get; init; }
    public required int RightMiss { get; init; }
    public required int LeftBadCuts { get; init; }
    public required int RightBadCuts { get; init; }
    public required int LeftBombs { get; init; }
    public required int RightBombs { get; init; }
}

public class AccuracyTracker
{
    public required float AccRight { get; init; }
    public required float AccLeft { get; init; }
    public required float LeftPreswing { get; init; }
    public required float RightPreswing { get; init; }
    public required float LeftPostSwing { get; init; }
    public required float RightPostSwing { get; init; }
    public required float LeftTimeDependence { get; init; }
    public required float RightTimeDependence { get; init; }
    public required List<float> LeftAverageCut { get; init; }
    public required List<float> RightAverageCut { get; init; }
    public required List<float> GridAcc { get; init; }
    public required float FcAcc { get; init; }
}

public class WinTracker
{
    public required bool Won { get; init; }
    public required float EndTime { get; init; }
    public required int NbOfPause { get; init; }
    public required float TotalPauseDuration { get; init; }
    public required float JumpDistance { get; init; }
    public required float AverageHeight { get; init; }
    public required AverageHeadPosition? AverageHeadPosition { get; init; }
    public required int TotalScore { get; init; }
    public required int MaxScore { get; init; }
}

public class AverageHeadPosition
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
}

public class ScoreGraphTracker
{
    public required List<float> Graph { get; init; }
}