using System.Linq.Expressions;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Api.Features.Scores.Http;

public static class ScoreMappers
{
    private static Func<AbstractScore, ScoreResponses.Score>? _mapScoreImpl;

    public static Expression<Func<BeatLeaderScore, ScoreResponses.ScoreStatistics?>> MapScoreStatisticsExpression =>
        blScore => blScore.Statistics == null
            ? null
            : new ScoreResponses.ScoreStatistics(
                new ScoreResponses.WinTracker(
                    blScore.Statistics.WinTracker.IsWin,
                    blScore.Statistics.WinTracker.EndTime,
                    blScore.Statistics.WinTracker.PauseCount,
                    blScore.Statistics.WinTracker.TotalPauseDuration,
                    blScore.Statistics.WinTracker.JumpDistance,
                    blScore.Statistics.WinTracker.AverageHeight,
                    blScore.Statistics.WinTracker.TotalScore,
                    blScore.Statistics.WinTracker.MaxScore,
                    blScore.Statistics.WinTracker.AverageHeadPosition == null
                        ? null
                        : new ScoreResponses.AverageHeadPosition(
                            blScore.Statistics.WinTracker.AverageHeadPosition.Value.X,
                            blScore.Statistics.WinTracker.AverageHeadPosition.Value.Y,
                            blScore.Statistics.WinTracker.AverageHeadPosition.Value.Z)),
                new ScoreResponses.HitTracker(
                    blScore.Statistics.HitTracker.Max115Streak,
                    blScore.Statistics.HitTracker.LeftTiming,
                    blScore.Statistics.HitTracker.RightTiming,
                    blScore.Statistics.HitTracker.LeftMiss,
                    blScore.Statistics.HitTracker.RightMiss,
                    blScore.Statistics.HitTracker.LeftBadCuts,
                    blScore.Statistics.HitTracker.RightBadCuts,
                    blScore.Statistics.HitTracker.LeftBombs,
                    blScore.Statistics.HitTracker.RightBombs),
                new ScoreResponses.AccuracyTracker(
                    blScore.Statistics.AccuracyTracker.AccRight,
                    blScore.Statistics.AccuracyTracker.AccLeft,
                    blScore.Statistics.AccuracyTracker.LeftPreSwing,
                    blScore.Statistics.AccuracyTracker.RightPreSwing,
                    blScore.Statistics.AccuracyTracker.LeftPostSwing,
                    blScore.Statistics.AccuracyTracker.RightPostSwing,
                    blScore.Statistics.AccuracyTracker.LeftTimeDependence,
                    blScore.Statistics.AccuracyTracker.RightTimeDependence,
                    blScore.Statistics.AccuracyTracker.LeftAverageCutGraphGrid,
                    blScore.Statistics.AccuracyTracker.RightAverageCutGraphGrid,
                    blScore.Statistics.AccuracyTracker.AccuracyGrid),
                new ScoreResponses.ScoreGraphTracker(blScore.Statistics.ScoreGraphTracker.Graph));

    [Expandable(nameof(MapScoreExpression))]
    public static ScoreResponses.Score Map(this AbstractScore self)
        => (_mapScoreImpl ??= MapScoreExpression().Compile())(self);

    private static Expression<Func<AbstractScore, ScoreResponses.Score>> MapScoreExpression()
        => score => score.Type == AbstractScore.EScoreType.BeatLeader
            ? new ScoreResponses.Score.BeatLeaderScore(
                score.Id,
                score.SongDifficultyId,
                score.BaseScore,
                score.Modifiers.Map(),
                score.SetAt,
                score.MaxCombo,
                score.IsFullCombo,
                score.MissedNotes,
                score.BadCuts,
                score.HMD.Map(),
                ((BeatLeaderScore)score).Statistics != null
                    ? ((BeatLeaderScore)score).Statistics!.WinTracker.PauseCount
                    : null,
                ((BeatLeaderScore)score).Statistics != null
                    ? ((BeatLeaderScore)score).Statistics!.WinTracker.TotalPauseDuration
                    : null,
                ((BeatLeaderScore)score).Statistics != null,
                ((BeatLeaderScore)score).BeatLeaderScoreId)
            : new ScoreResponses.Score.ScoreSaberScore(
                score.Id,
                score.SongDifficultyId,
                score.BaseScore,
                score.Modifiers.Map(),
                score.SetAt,
                score.MaxCombo,
                score.IsFullCombo,
                score.MissedNotes,
                score.BadCuts,
                score.HMD.Map(),
                ((ScoreSaberScore)score).ScoreSaberScoreId,
                ((ScoreSaberScore)score).DeviceHmd,
                ((ScoreSaberScore)score).DeviceControllerLeft,
                ((ScoreSaberScore)score).DeviceControllerRight);
}