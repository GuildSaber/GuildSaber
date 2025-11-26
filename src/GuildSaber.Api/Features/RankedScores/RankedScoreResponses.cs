using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoreResponses
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(BeatLeaderScore), "BeatLeader")]
    [JsonDerivedType(typeof(ScoreSaberScore), "ScoreSaber")]
    public abstract record Score(
        long Id,
        int BaseScore,
        RankedMapRequest.EModifiers Modifiers,
        DateTimeOffset SetAt,
        int? MaxCombo,
        bool IsFullCombo,
        int MissedNotes,
        int BadCuts,
        EHMD HMD)
    {
        public sealed record BeatLeaderScore(
            long Id,
            int BaseScore,
            RankedMapRequest.EModifiers Modifiers,
            DateTimeOffset SetAt,
            int? MaxCombo,
            bool IsFullCombo,
            int MissedNotes,
            int BadCuts,
            EHMD HMD,
            //ScoreStatistics? Statistics,
            BeatLeaderScoreId? BeatLeaderScoreId
        ) : Score(Id, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts, HMD);

        public sealed record ScoreSaberScore(
            long Id,
            int BaseScore,
            RankedMapRequest.EModifiers Modifiers,
            DateTimeOffset SetAt,
            int? MaxCombo,
            bool IsFullCombo,
            int MissedNotes,
            int BadCuts,
            EHMD HMD,
            ScoreSaberScoreId ScoreSaberScoreId,
            string? DeviceHmd,
            string? DeviceControllerLeft,
            string? DeviceControllerRight
        ) : Score(Id, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts, HMD);
    }

    public sealed record RankedScore(
        long Id,
        long RankedMapId,
        Score Score,
        Score? PrevScore,
        EState State,
        int Rank,
        float RawPoints,
        int EffectiveScore
    );

    public record RankedScoreWithPlayer(
        RankedScore RankedScore,
        PlayerResponses.Player Player
    );

    public record RankedScoreWithRankedMap(
        RankedScore RankedScore,
        RankedMapResponses.RankedMap RankedMap
    );

    public record ScoreStatistics(
        WinTracker WinTracker,
        HitTracker HitTracker,
        AccuracyTracker AccuracyTracker,
        ScoreGraphTracker ScoreGraphTracker
    );

    public readonly record struct AverageHeadPosition(float X, float Y, float Z);

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

    [Flags]
    public enum EState
    {
        None = 0,
        Selected = 1 << 0,
        Denied = 1 << 1,
        Removed = 1 << 2,
        Pending = 1 << 3,
        Confirmed = 1 << 4,
        Refused = 1 << 5
    }

    public enum EHMD
    {
        Unknown = 0,
        Rift = 1,
        Vive = 2,
        VivePro = 4,
        WMR = 8,
        RiftS = 16,
        Quest = 32,
        Index = 64,
        ViveCosmos = 128,
        Quest2 = 256,
        Quest3 = 512,
        Quest3S = 513,

        PicoNeo3 = 33,
        PicoNeo2 = 34,
        VivePro2 = 35,
        ViveElite = 36,
        Miramar = 37,
        Pimax8K = 38,
        Pimax5K = 39,
        PimaxArtisan = 40,
        HpReverb = 41,
        SamsungWMR = 42,
        QiyuDream = 43,
        Disco = 44,
        LenovoExplorer = 45,
        AcerWMR = 46,
        ViveFocus = 47,
        Arpara = 48,
        DellVisor = 49,
        E3 = 50,
        ViveDvt = 51,
        Glasses20 = 52,
        Hedy = 53,
        Vaporeon = 54,
        Huaweivr = 55,
        AsusWMR = 56,
        CloudXR = 57,
        Vridge = 58,
        Medion = 59,
        PicoNeo4 = 60,
        QuestPro = 61,
        PimaxCrystal = 62,
        E4 = 63,
        Controllable = 65,
        BigScreenBeyond = 66,
        Nolosonic = 67,
        Hypereal = 68,
        Varjoaero = 69,
        PSVR2 = 70,
        Megane1 = 71,
        VarjoXR3 = 72
    }
}