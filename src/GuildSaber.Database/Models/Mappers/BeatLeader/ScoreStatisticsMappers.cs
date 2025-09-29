using GuildSaber.Database.Models.Server.Scores;
using BLScoreStatistics = GuildSaber.Common.Services.BeatLeader.Models.Responses.ScoreStatistics;
using BLHitTracker = GuildSaber.Common.Services.BeatLeader.Models.Responses.HitTracker;
using BLAccuracyTracker = GuildSaber.Common.Services.BeatLeader.Models.Responses.AccuracyTracker;
using BLWinTracker = GuildSaber.Common.Services.BeatLeader.Models.Responses.WinTracker;
using BLScoreGraphTracker = GuildSaber.Common.Services.BeatLeader.Models.Responses.ScoreGraphTracker;
using BLAverageHeadPosition = GuildSaber.Common.Services.BeatLeader.Models.Responses.AverageHeadPosition;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public static class ScoreStatisticsMappers
{
    public static ScoreStatistics? Map(this BLScoreStatistics? self) => self is null
        ? null
        : new ScoreStatistics
        {
            HitTracker = self.HitTracker.Map(),
            AccuracyTracker = self.AccuracyTracker.Map(),
            WinTracker = self.WinTracker.Map(),
            ScoreGraphTracker = self.ScoreGraphTracker.Map()
        };

    private static HitTracker Map(this BLHitTracker self) => new(
        Max115Streak: self.MaxStreak,
        LeftTiming: self.LeftTiming,
        RightTiming: self.RightTiming,
        LeftMiss: self.LeftMiss,
        RightMiss: self.RightMiss,
        LeftBadCuts: self.LeftBadCuts,
        RightBadCuts: self.RightBadCuts,
        LeftBombs: self.LeftBombs,
        RightBombs: self.RightBombs
    );

    private static AccuracyTracker Map(this BLAccuracyTracker self) => new(
        AccRight: self.AccRight,
        AccLeft: self.AccLeft,
        LeftPreswing: self.LeftPreswing,
        RightPreswing: self.RightPreswing,
        LeftPostSwing: self.LeftPostSwing,
        RightPostSwing: self.RightPostSwing,
        LeftTimeDependence: self.LeftTimeDependence,
        RightTimeDependence: self.RightTimeDependence,
        LeftAverageCutGraphGrid: self.LeftAverageCut,
        RightAverageCutGrid: self.RightAverageCut,
        AccuracyGrid: self.GridAcc
    );

    private static WinTracker Map(this BLWinTracker self) => new(
        IsWin: self.Won,
        EndTime: self.EndTime,
        PauseCount: self.NbOfPause,
        TotalPauseDuration: self.TotalPauseDuration,
        JumpDistance: self.JumpDistance,
        AverageHeight: self.AverageHeight,
        TotalScore: self.TotalScore,
        MaxScore: self.MaxScore)
    {
        AverageHeadPosition = self.AverageHeadPosition.Map()
    };

    private static ScoreGraphTracker Map(this BLScoreGraphTracker self) => new(self.Graph);

    private static AverageHeadPosition? Map(this BLAverageHeadPosition? self)
        => self is null ? null : new AverageHeadPosition(self.X, self.Y, self.Z);
}