using System.Linq.Expressions;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Scores.Http;
using GuildSaber.Database.Models.Server.Players;
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

    public static ScoreResponses.EHMD Map(this PlayerHardwareInfo.EHMD self) => self switch
    {
        PlayerHardwareInfo.EHMD.Unknown => ScoreResponses.EHMD.Unknown,
        PlayerHardwareInfo.EHMD.Rift => ScoreResponses.EHMD.Rift,
        PlayerHardwareInfo.EHMD.Vive => ScoreResponses.EHMD.Vive,
        PlayerHardwareInfo.EHMD.VivePro => ScoreResponses.EHMD.VivePro,
        PlayerHardwareInfo.EHMD.WMR => ScoreResponses.EHMD.WMR,
        PlayerHardwareInfo.EHMD.RiftS => ScoreResponses.EHMD.RiftS,
        PlayerHardwareInfo.EHMD.Quest => ScoreResponses.EHMD.Quest,
        PlayerHardwareInfo.EHMD.Index => ScoreResponses.EHMD.Index,
        PlayerHardwareInfo.EHMD.ViveCosmos => ScoreResponses.EHMD.ViveCosmos,
        PlayerHardwareInfo.EHMD.Quest2 => ScoreResponses.EHMD.Quest2,
        PlayerHardwareInfo.EHMD.Quest3 => ScoreResponses.EHMD.Quest3,
        PlayerHardwareInfo.EHMD.Quest3S => ScoreResponses.EHMD.Quest3S,
        PlayerHardwareInfo.EHMD.PicoNeo3 => ScoreResponses.EHMD.PicoNeo3,
        PlayerHardwareInfo.EHMD.PicoNeo2 => ScoreResponses.EHMD.PicoNeo2,
        PlayerHardwareInfo.EHMD.VivePro2 => ScoreResponses.EHMD.VivePro2,
        PlayerHardwareInfo.EHMD.ViveElite => ScoreResponses.EHMD.ViveElite,
        PlayerHardwareInfo.EHMD.Miramar => ScoreResponses.EHMD.Miramar,
        PlayerHardwareInfo.EHMD.Pimax8K => ScoreResponses.EHMD.Pimax8K,
        PlayerHardwareInfo.EHMD.Pimax5K => ScoreResponses.EHMD.Pimax5K,
        PlayerHardwareInfo.EHMD.PimaxArtisan => ScoreResponses.EHMD.PimaxArtisan,
        PlayerHardwareInfo.EHMD.HpReverb => ScoreResponses.EHMD.HpReverb,
        PlayerHardwareInfo.EHMD.SamsungWMR => ScoreResponses.EHMD.SamsungWMR,
        PlayerHardwareInfo.EHMD.QiyuDream => ScoreResponses.EHMD.QiyuDream,
        PlayerHardwareInfo.EHMD.Disco => ScoreResponses.EHMD.Disco,
        PlayerHardwareInfo.EHMD.LenovoExplorer => ScoreResponses.EHMD.LenovoExplorer,
        PlayerHardwareInfo.EHMD.AcerWMR => ScoreResponses.EHMD.AcerWMR,
        PlayerHardwareInfo.EHMD.ViveFocus => ScoreResponses.EHMD.ViveFocus,
        PlayerHardwareInfo.EHMD.Arpara => ScoreResponses.EHMD.Arpara,
        PlayerHardwareInfo.EHMD.DellVisor => ScoreResponses.EHMD.DellVisor,
        PlayerHardwareInfo.EHMD.E3 => ScoreResponses.EHMD.E3,
        PlayerHardwareInfo.EHMD.ViveDvt => ScoreResponses.EHMD.ViveDvt,
        PlayerHardwareInfo.EHMD.Glasses20 => ScoreResponses.EHMD.Glasses20,
        PlayerHardwareInfo.EHMD.Hedy => ScoreResponses.EHMD.Hedy,
        PlayerHardwareInfo.EHMD.Vaporeon => ScoreResponses.EHMD.Vaporeon,
        PlayerHardwareInfo.EHMD.Huaweivr => ScoreResponses.EHMD.Huaweivr,
        PlayerHardwareInfo.EHMD.AsusWMR => ScoreResponses.EHMD.AsusWMR,
        PlayerHardwareInfo.EHMD.CloudXR => ScoreResponses.EHMD.CloudXR,
        PlayerHardwareInfo.EHMD.Vridge => ScoreResponses.EHMD.Vridge,
        PlayerHardwareInfo.EHMD.Medion => ScoreResponses.EHMD.Medion,
        PlayerHardwareInfo.EHMD.PicoNeo4 => ScoreResponses.EHMD.PicoNeo4,
        PlayerHardwareInfo.EHMD.QuestPro => ScoreResponses.EHMD.QuestPro,
        PlayerHardwareInfo.EHMD.PimaxCrystal => ScoreResponses.EHMD.PimaxCrystal,
        PlayerHardwareInfo.EHMD.E4 => ScoreResponses.EHMD.E4,
        PlayerHardwareInfo.EHMD.Controllable => ScoreResponses.EHMD.Controllable,
        PlayerHardwareInfo.EHMD.BigScreenBeyond => ScoreResponses.EHMD.BigScreenBeyond,
        PlayerHardwareInfo.EHMD.Nolosonic => ScoreResponses.EHMD.Nolosonic,
        PlayerHardwareInfo.EHMD.Hypereal => ScoreResponses.EHMD.Hypereal,
        PlayerHardwareInfo.EHMD.Varjoaero => ScoreResponses.EHMD.Varjoaero,
        PlayerHardwareInfo.EHMD.PSVR2 => ScoreResponses.EHMD.PSVR2,
        PlayerHardwareInfo.EHMD.Megane1 => ScoreResponses.EHMD.Megane1,
        PlayerHardwareInfo.EHMD.VarjoXR3 => ScoreResponses.EHMD.VarjoXR3,
        _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
}