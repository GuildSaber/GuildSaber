using System.Linq.Expressions;
using GuildSaber.Api.Features.Hardware.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Api.Features.Scores.Http;

public static class ScoreMappers
{
    private static Func<AbstractScore, ScoreResponses.Score>? _mapScoreImpl;

    public static Expression<Func<AbstractScore, ScoreResponses.ScoreStatistics?>> MapScoreStatisticsExpression =>
        score => score.Statistics == null
            ? null
            : new ScoreResponses.ScoreStatistics(
                new ScoreResponses.WinTracker(
                    score.Statistics.WinTracker.IsWin,
                    score.Statistics.WinTracker.EndTime,
                    score.Statistics.WinTracker.PauseStatistics == null
                        ? null
                        : new ScoreResponses.PauseStatistics(
                            score.Statistics.WinTracker.PauseStatistics.PauseCount,
                            score.Statistics.WinTracker.PauseStatistics.TotalPauseDuration),
                    score.Statistics.WinTracker.JumpDistance,
                    score.Statistics.WinTracker.AverageHeight,
                    score.Statistics.WinTracker.TotalScore,
                    score.Statistics.WinTracker.MaxScore,
                    score.Statistics.WinTracker.AverageHeadPosition == null
                        ? null
                        : new ScoreResponses.AverageHeadPosition(
                            score.Statistics.WinTracker.AverageHeadPosition.Value.X,
                            score.Statistics.WinTracker.AverageHeadPosition.Value.Y,
                            score.Statistics.WinTracker.AverageHeadPosition.Value.Z)),
                new ScoreResponses.HitTracker(
                    score.Statistics.HitTracker.Max115Streak,
                    score.Statistics.HitTracker.LeftTiming,
                    score.Statistics.HitTracker.RightTiming,
                    score.Statistics.HitTracker.LeftMiss,
                    score.Statistics.HitTracker.RightMiss,
                    score.Statistics.HitTracker.LeftBadCuts,
                    score.Statistics.HitTracker.RightBadCuts,
                    score.Statistics.HitTracker.LeftBombs,
                    score.Statistics.HitTracker.RightBombs),
                new ScoreResponses.AccuracyTracker(
                    score.Statistics.AccuracyTracker.AccRight,
                    score.Statistics.AccuracyTracker.AccLeft,
                    score.Statistics.AccuracyTracker.LeftPreSwing,
                    score.Statistics.AccuracyTracker.RightPreSwing,
                    score.Statistics.AccuracyTracker.LeftPostSwing,
                    score.Statistics.AccuracyTracker.RightPostSwing,
                    score.Statistics.AccuracyTracker.LeftTimeDependence,
                    score.Statistics.AccuracyTracker.RightTimeDependence,
                    score.Statistics.AccuracyTracker.LeftAverageCutGraphGrid,
                    score.Statistics.AccuracyTracker.RightAverageCutGraphGrid,
                    score.Statistics.AccuracyTracker.AccuracyGrid),
                new ScoreResponses.ScoreGraphTracker(score.Statistics.ScoreGraphTracker.Graph));

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
                new ScoreResponses.Devices(
                    score.Hmd.Map(),
                    score.LeftController.Map(),
                    score.RightController.Map()),
                score.Statistics != null && score.Statistics.WinTracker.PauseStatistics != null
                    ? new ScoreResponses.PauseStatistics(
                        score.Statistics.WinTracker.PauseStatistics.PauseCount,
                        score.Statistics.WinTracker.PauseStatistics.TotalPauseDuration)
                    : null,
                score.Statistics != null,
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
                new ScoreResponses.Devices(
                    score.Hmd.Map(),
                    score.LeftController.Map(),
                    score.RightController.Map()),
                score.Statistics != null && score.Statistics.WinTracker.PauseStatistics != null
                    ? new ScoreResponses.PauseStatistics(
                        score.Statistics.WinTracker.PauseStatistics.PauseCount,
                        score.Statistics.WinTracker.PauseStatistics.TotalPauseDuration)
                    : null,
                score.Statistics != null,
                ((ScoreSaberScore)score).ScoreSaberScoreId);
}