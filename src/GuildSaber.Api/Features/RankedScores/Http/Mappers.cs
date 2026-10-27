using System.Linq.Expressions;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Scores.Http;
using GuildSaber.Database.Models.Server.RankedScores;

namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreMappers
{
    private static Func<RankedScore, RankedScoreResponses.RankedScore>? _mapRankedScoreImpl;

    public static Expression<Func<RankedScore, RankedScoreResponses.RankedScoreWithPlayer>>
        MapRankedScoreWithPlayerExpression => rankedScore => new RankedScoreResponses.RankedScoreWithPlayer(
        rankedScore.Map(),
        rankedScore.Player.Map()
    );

    public static Expression<Func<RankedScore, RankedScoreResponses.RankedScoreWithRankedMap>>
        MapRankedScoreWithRankedMapExpression => rankedScore => new RankedScoreResponses.RankedScoreWithRankedMap(
        rankedScore.Map(),
        rankedScore.RankedMap.Map()
    );

    [Expandable(nameof(MapRankedScoreExpression))]
    public static RankedScoreResponses.RankedScore Map(this RankedScore self)
        => (_mapRankedScoreImpl ??= MapRankedScoreExpression().Compile())(self);

    public static Expression<Func<RankedScore, RankedScoreResponses.RankedScore>> MapRankedScoreExpression()
        => rankedScore => rankedScore.Type == RankedScore.ERankedScoreType.Valid
            ? new RankedScoreResponses.RankedScore.ValidRankedScore(
                rankedScore.Id,
                rankedScore.PlayerId,
                rankedScore.PointId,
                rankedScore.RankedMapId,
                rankedScore.EditedAt,
                rankedScore.Score.Map(),
                rankedScore.PrevScore == null ? null : rankedScore.PrevScore.Map(),
                rankedScore.IsSelected,
                ((ValidRankedScore)rankedScore).RawPoints,
                rankedScore.EffectiveScore,
                ((ValidRankedScore)rankedScore).Rank)
            : rankedScore.Type == RankedScore.ERankedScoreType.Accepted
                ? new RankedScoreResponses.RankedScore.AcceptedRankedScore(
                    rankedScore.Id,
                    rankedScore.PlayerId,
                    rankedScore.PointId,
                    rankedScore.RankedMapId,
                    rankedScore.EditedAt,
                    rankedScore.Score.Map(),
                    rankedScore.PrevScore == null ? null : rankedScore.PrevScore.Map(),
                    rankedScore.IsSelected,
                    ((AcceptedRankedScore)rankedScore).RawPoints,
                    rankedScore.EffectiveScore,
                    ((AcceptedRankedScore)rankedScore).Rank)
                : rankedScore.Type == RankedScore.ERankedScoreType.Pending
                    ? new RankedScoreResponses.RankedScore.PendingRankedScore(
                        rankedScore.Id,
                        rankedScore.PlayerId,
                        rankedScore.PointId,
                        rankedScore.RankedMapId,
                        rankedScore.EditedAt,
                        rankedScore.Score.Map(),
                        rankedScore.PrevScore == null ? null : rankedScore.PrevScore.Map(),
                        rankedScore.IsSelected,
                        ((PendingRankedScore)rankedScore).RawPoints,
                        rankedScore.EffectiveScore)
                    : rankedScore.Type == RankedScore.ERankedScoreType.Refused
                        ? new RankedScoreResponses.RankedScore.RefusedRankedScore(
                            rankedScore.Id,
                            rankedScore.PlayerId,
                            rankedScore.PointId,
                            rankedScore.RankedMapId,
                            rankedScore.EditedAt,
                            rankedScore.Score.Map(),
                            rankedScore.PrevScore == null ? null : rankedScore.PrevScore.Map(),
                            rankedScore.IsSelected,
                            ((RefusedRankedScore)rankedScore).RawPoints,
                            rankedScore.EffectiveScore)
                        : new RankedScoreResponses.RankedScore.InvalidRankedScore(
                            rankedScore.Id,
                            rankedScore.PlayerId,
                            rankedScore.PointId,
                            rankedScore.RankedMapId,
                            rankedScore.EditedAt,
                            rankedScore.Score.Map(),
                            rankedScore.PrevScore == null ? null : rankedScore.PrevScore.Map(),
                            rankedScore.IsSelected,
                            rankedScore.EffectiveScore,
                            ((InvalidRankedScore)rankedScore).InvalidReason.Map());

    public static RankedScoreResponses.EInvalidReason Map(this InvalidRankedScore.EInvalidReason self) =>
        Enum.GetValues<InvalidRankedScore.EInvalidReason>()
            .Where(flag => flag != InvalidRankedScore.EInvalidReason.Unspecified && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                InvalidRankedScore.EInvalidReason.Unspecified => RankedScoreResponses.EInvalidReason.Unspecified,
                InvalidRankedScore.EInvalidReason.MinAccuracyRequirements
                    => RankedScoreResponses.EInvalidReason.MinAccuracyRequirements,
                InvalidRankedScore.EInvalidReason.ProhibitedModifiers
                    => RankedScoreResponses.EInvalidReason.ProhibitedModifiers,
                InvalidRankedScore.EInvalidReason.MissingModifiers
                    => RankedScoreResponses.EInvalidReason.MissingModifiers,
                InvalidRankedScore.EInvalidReason.PausedTooMuch => RankedScoreResponses.EInvalidReason.PausedTooMuch,
                InvalidRankedScore.EInvalidReason.NoFullCombo => RankedScoreResponses.EInvalidReason.NoFullCombo,
                InvalidRankedScore.EInvalidReason.MissingTrackers
                    => RankedScoreResponses.EInvalidReason.MissingTrackers,
                _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null)
            })
            .Aggregate(RankedScoreResponses.EInvalidReason.Unspecified, (acc, mapped) => acc | mapped);
}