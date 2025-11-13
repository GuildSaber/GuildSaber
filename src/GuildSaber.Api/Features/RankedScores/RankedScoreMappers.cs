using System.Linq.Expressions;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoreMappers
{
    public static RankedScoreResponses.Score Map(this AbstractScore self) => self switch
    {
        BeatLeaderScore blScore => new RankedScoreResponses.Score.BeatLeaderScore(
            self.Id,
            self.BaseScore,
            self.Modifiers.Map(),
            self.SetAt,
            self.MaxCombo,
            self.IsFullCombo,
            self.MissedNotes,
            self.BadCuts,
            self.HMD.Map(),
            /*blScore.Statistics == null
                ? null
                : new RankedScoreResponses.ScoreStatistics(
                    new RankedScoreResponses.WinTracker(
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
                            : new RankedScoreResponses.AverageHeadPosition(
                                blScore.Statistics.WinTracker.AverageHeadPosition.Value.X,
                                blScore.Statistics.WinTracker.AverageHeadPosition.Value.Y,
                                blScore.Statistics.WinTracker.AverageHeadPosition.Value.Z)),
                    new RankedScoreResponses.HitTracker(
                        blScore.Statistics.HitTracker.Max115Streak,
                        blScore.Statistics.HitTracker.LeftTiming,
                        blScore.Statistics.HitTracker.RightTiming,
                        blScore.Statistics.HitTracker.LeftMiss,
                        blScore.Statistics.HitTracker.RightMiss,
                        blScore.Statistics.HitTracker.LeftBadCuts,
                        blScore.Statistics.HitTracker.RightBadCuts,
                        blScore.Statistics.HitTracker.LeftBombs,
                        blScore.Statistics.HitTracker.RightBombs),
                    new RankedScoreResponses.AccuracyTracker(
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
                    new RankedScoreResponses.ScoreGraphTracker(blScore.Statistics.ScoreGraphTracker.Graph)),*/
            blScore.BeatLeaderScoreId
        ),
        ScoreSaberScore ssScore => new RankedScoreResponses.Score.ScoreSaberScore(
            self.Id,
            self.BaseScore,
            self.Modifiers.Map(),
            self.SetAt,
            self.MaxCombo,
            self.IsFullCombo,
            self.MissedNotes,
            self.BadCuts,
            self.HMD.Map(),
            ssScore.ScoreSaberScoreId,
            ssScore.DeviceHmd,
            ssScore.DeviceControllerLeft,
            ssScore.DeviceControllerRight),
        _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };

    public static Expression<Func<RankedScore, RankedScoreResponses.RankedScoreWithPlayer>>
        MapRankedScoreWithPlayerExpression(ServerDbContext dbContext)
        => self => new RankedScoreResponses.RankedScoreWithPlayer(
            self.Id,
            self.RankedMapId,
            self.Score.Map(),
            null,
            self.State.Map(),
            self.Rank,
            self.RawPoints,
            self.EffectiveScore,
            dbContext.Players.Where(p => p.Id == self.PlayerId)
                .Select(PlayerMappers.MapPlayerExpression)
                .First()
        );

    public static RankedScoreResponses.EState Map(this RankedScore.EState self) =>
        Enum.GetValues<RankedScore.EState>()
            .Where(flag => flag != RankedScore.EState.None && self.HasFlag(flag))
            .Select(flag => flag switch
            {
                RankedScore.EState.None => RankedScoreResponses.EState.None,
                RankedScore.EState.Denied => RankedScoreResponses.EState.Denied,
                RankedScore.EState.Selected => RankedScoreResponses.EState.Selected,
                RankedScore.EState.Removed => RankedScoreResponses.EState.Removed,
                RankedScore.EState.Pending => RankedScoreResponses.EState.Pending,
                RankedScore.EState.Confirmed => RankedScoreResponses.EState.Confirmed,
                RankedScore.EState.Refused => RankedScoreResponses.EState.Refused,
                // On purpose, because it will construct with each flag separately.
                RankedScore.EState.NonPointGiving => RankedScoreResponses.EState.None,
                _ => throw new ArgumentOutOfRangeException(nameof(flag), flag, null)
            })
            .Aggregate(RankedScoreResponses.EState.None, (acc, mapped) => acc | mapped);

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