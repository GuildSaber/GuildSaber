using System.Linq.Expressions;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreMappers
{
    private static Func<AbstractScore, RankedScoreResponses.Score>? _mapScoreImpl;

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

    [Expandable(nameof(MapScoreExpression))]
    public static RankedScoreResponses.Score Map(this AbstractScore self)
        => (_mapScoreImpl ??= MapScoreExpression().Compile())(self);

    private static Expression<Func<AbstractScore, RankedScoreResponses.Score>> MapScoreExpression()
        => score => score.Type == AbstractScore.EScoreType.BeatLeader
            ? new RankedScoreResponses.Score.BeatLeaderScore(
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
                ((BeatLeaderScore)score).BeatLeaderScoreId)
            : new RankedScoreResponses.Score.ScoreSaberScore(
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

    public static RankedScoreResponses.EHMD Map(this PlayerHardwareInfo.EHMD self) => self switch
    {
        PlayerHardwareInfo.EHMD.Unknown => RankedScoreResponses.EHMD.Unknown,
        PlayerHardwareInfo.EHMD.Rift => RankedScoreResponses.EHMD.Rift,
        PlayerHardwareInfo.EHMD.Vive => RankedScoreResponses.EHMD.Vive,
        PlayerHardwareInfo.EHMD.VivePro => RankedScoreResponses.EHMD.VivePro,
        PlayerHardwareInfo.EHMD.WMR => RankedScoreResponses.EHMD.WMR,
        PlayerHardwareInfo.EHMD.RiftS => RankedScoreResponses.EHMD.RiftS,
        PlayerHardwareInfo.EHMD.Quest => RankedScoreResponses.EHMD.Quest,
        PlayerHardwareInfo.EHMD.Index => RankedScoreResponses.EHMD.Index,
        PlayerHardwareInfo.EHMD.ViveCosmos => RankedScoreResponses.EHMD.ViveCosmos,
        PlayerHardwareInfo.EHMD.Quest2 => RankedScoreResponses.EHMD.Quest2,
        PlayerHardwareInfo.EHMD.Quest3 => RankedScoreResponses.EHMD.Quest3,
        PlayerHardwareInfo.EHMD.Quest3S => RankedScoreResponses.EHMD.Quest3S,
        PlayerHardwareInfo.EHMD.PicoNeo3 => RankedScoreResponses.EHMD.PicoNeo3,
        PlayerHardwareInfo.EHMD.PicoNeo2 => RankedScoreResponses.EHMD.PicoNeo2,
        PlayerHardwareInfo.EHMD.VivePro2 => RankedScoreResponses.EHMD.VivePro2,
        PlayerHardwareInfo.EHMD.ViveElite => RankedScoreResponses.EHMD.ViveElite,
        PlayerHardwareInfo.EHMD.Miramar => RankedScoreResponses.EHMD.Miramar,
        PlayerHardwareInfo.EHMD.Pimax8K => RankedScoreResponses.EHMD.Pimax8K,
        PlayerHardwareInfo.EHMD.Pimax5K => RankedScoreResponses.EHMD.Pimax5K,
        PlayerHardwareInfo.EHMD.PimaxArtisan => RankedScoreResponses.EHMD.PimaxArtisan,
        PlayerHardwareInfo.EHMD.HpReverb => RankedScoreResponses.EHMD.HpReverb,
        PlayerHardwareInfo.EHMD.SamsungWMR => RankedScoreResponses.EHMD.SamsungWMR,
        PlayerHardwareInfo.EHMD.QiyuDream => RankedScoreResponses.EHMD.QiyuDream,
        PlayerHardwareInfo.EHMD.Disco => RankedScoreResponses.EHMD.Disco,
        PlayerHardwareInfo.EHMD.LenovoExplorer => RankedScoreResponses.EHMD.LenovoExplorer,
        PlayerHardwareInfo.EHMD.AcerWMR => RankedScoreResponses.EHMD.AcerWMR,
        PlayerHardwareInfo.EHMD.ViveFocus => RankedScoreResponses.EHMD.ViveFocus,
        PlayerHardwareInfo.EHMD.Arpara => RankedScoreResponses.EHMD.Arpara,
        PlayerHardwareInfo.EHMD.DellVisor => RankedScoreResponses.EHMD.DellVisor,
        PlayerHardwareInfo.EHMD.E3 => RankedScoreResponses.EHMD.E3,
        PlayerHardwareInfo.EHMD.ViveDvt => RankedScoreResponses.EHMD.ViveDvt,
        PlayerHardwareInfo.EHMD.Glasses20 => RankedScoreResponses.EHMD.Glasses20,
        PlayerHardwareInfo.EHMD.Hedy => RankedScoreResponses.EHMD.Hedy,
        PlayerHardwareInfo.EHMD.Vaporeon => RankedScoreResponses.EHMD.Vaporeon,
        PlayerHardwareInfo.EHMD.Huaweivr => RankedScoreResponses.EHMD.Huaweivr,
        PlayerHardwareInfo.EHMD.AsusWMR => RankedScoreResponses.EHMD.AsusWMR,
        PlayerHardwareInfo.EHMD.CloudXR => RankedScoreResponses.EHMD.CloudXR,
        PlayerHardwareInfo.EHMD.Vridge => RankedScoreResponses.EHMD.Vridge,
        PlayerHardwareInfo.EHMD.Medion => RankedScoreResponses.EHMD.Medion,
        PlayerHardwareInfo.EHMD.PicoNeo4 => RankedScoreResponses.EHMD.PicoNeo4,
        PlayerHardwareInfo.EHMD.QuestPro => RankedScoreResponses.EHMD.QuestPro,
        PlayerHardwareInfo.EHMD.PimaxCrystal => RankedScoreResponses.EHMD.PimaxCrystal,
        PlayerHardwareInfo.EHMD.E4 => RankedScoreResponses.EHMD.E4,
        PlayerHardwareInfo.EHMD.Controllable => RankedScoreResponses.EHMD.Controllable,
        PlayerHardwareInfo.EHMD.BigScreenBeyond => RankedScoreResponses.EHMD.BigScreenBeyond,
        PlayerHardwareInfo.EHMD.Nolosonic => RankedScoreResponses.EHMD.Nolosonic,
        PlayerHardwareInfo.EHMD.Hypereal => RankedScoreResponses.EHMD.Hypereal,
        PlayerHardwareInfo.EHMD.Varjoaero => RankedScoreResponses.EHMD.Varjoaero,
        PlayerHardwareInfo.EHMD.PSVR2 => RankedScoreResponses.EHMD.PSVR2,
        PlayerHardwareInfo.EHMD.Megane1 => RankedScoreResponses.EHMD.Megane1,
        PlayerHardwareInfo.EHMD.VarjoXR3 => RankedScoreResponses.EHMD.VarjoXR3,
        _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
}
