using System.ComponentModel;
using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;

namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreResponses
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(BeatLeaderScore), "BeatLeader")]
    [JsonDerivedType(typeof(ScoreSaberScore), "ScoreSaber")]
    public abstract record Score(
        long Id,
        long SongDifficultyId,
        int BaseScore,
        RankedMapRequests.EModifiers Modifiers,
        DateTimeOffset SetAt,
        int? MaxCombo,
        bool IsFullCombo,
        int MissedNotes,
        int BadCuts,
        EHMD HMD)
    {
        public sealed record BeatLeaderScore(
            long Id,
            long SongDifficultyId,
            int BaseScore,
            RankedMapRequests.EModifiers Modifiers,
            DateTimeOffset SetAt,
            int? MaxCombo,
            bool IsFullCombo,
            int MissedNotes,
            int BadCuts,
            EHMD HMD,
            //ScoreStatistics? Statistics,
            BeatLeaderScoreId? BeatLeaderScoreId
        ) : Score(Id, SongDifficultyId, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts, HMD);

        public sealed record ScoreSaberScore(
            long Id,
            long SongDifficultyId,
            int BaseScore,
            RankedMapRequests.EModifiers Modifiers,
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
        ) : Score(Id, SongDifficultyId, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts, HMD);
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ValidRankedScore), "Valid")]
    [JsonDerivedType(typeof(PendingRankedScore), "Pending")]
    [JsonDerivedType(typeof(AcceptedRankedScore), "Accepted")]
    [JsonDerivedType(typeof(RefusedRankedScore), "Refused")]
    [JsonDerivedType(typeof(InvalidRankedScore), "Invalid")]
    public abstract record RankedScore(
        RankedScoreId Id,
        PlayerId PlayerId,
        int PointId,
        long RankedMapId,
        DateTimeOffset EditedAt,
        Score Score,
        Score? PrevScore,
        bool IsSelected,
        int EffectiveScore
    )
    {
        public sealed record ValidRankedScore(
            RankedScoreId Id,
            PlayerId PlayerId,
            int PointId,
            long RankedMapId,
            DateTimeOffset EditedAt,
            Score Score,
            Score? PrevScore,
            bool IsSelected,
            float RawPoints,
            int EffectiveScore,
            int Rank
        ) : RankedScore(Id, PlayerId, PointId, RankedMapId, EditedAt, Score, PrevScore, IsSelected, EffectiveScore);

        public sealed record PendingRankedScore(
            RankedScoreId Id,
            PlayerId PlayerId,
            int PointId,
            long RankedMapId,
            DateTimeOffset EditedAt,
            Score Score,
            Score? PrevScore,
            bool IsSelected,
            float RawPoints,
            int EffectiveScore
        ) : RankedScore(Id, PlayerId, PointId, RankedMapId, EditedAt, Score, PrevScore, IsSelected, EffectiveScore);

        public sealed record AcceptedRankedScore(
            RankedScoreId Id,
            PlayerId PlayerId,
            int PointId,
            long RankedMapId,
            DateTimeOffset EditedAt,
            Score Score,
            Score? PrevScore,
            bool IsSelected,
            float RawPoints,
            int EffectiveScore,
            int Rank
        ) : RankedScore(Id, PlayerId, PointId, RankedMapId, EditedAt, Score, PrevScore, IsSelected, EffectiveScore);

        public sealed record RefusedRankedScore(
            RankedScoreId Id,
            PlayerId PlayerId,
            int PointId,
            long RankedMapId,
            DateTimeOffset EditedAt,
            Score Score,
            Score? PrevScore,
            bool IsSelected,
            float RawPoints,
            int EffectiveScore
        ) : RankedScore(Id, PlayerId, PointId, RankedMapId, EditedAt, Score, PrevScore, IsSelected, EffectiveScore);

        public sealed record InvalidRankedScore(
            RankedScoreId Id,
            PlayerId PlayerId,
            int PointId,
            long RankedMapId,
            DateTimeOffset EditedAt,
            Score Score,
            Score? PrevScore,
            bool IsSelected,
            int EffectiveScore,
            EInvalidReason InvalidReason
        ) : RankedScore(Id, PlayerId, PointId, RankedMapId, EditedAt, Score, PrevScore, IsSelected, EffectiveScore);
    }

    public record RankedScoreWithPlayer(
        RankedScore RankedScore,
        PlayerResponses.Player Player
    );

    public record RankedScoreWithRankedMap(
        RankedScore RankedScore,
        RankedMapResponses.RankedMap RankedMap
    );

    [Flags]
    public enum EInvalidReason
    {
        [Description("No reason specified.")]
        Unspecified = 0,

        [Description("Score did not meet the minimum score requirement.")]
        MinAccuracyRequirements = 1 << 0,

        [Description("Score used prohibited modifiers.")]
        ProhibitedModifiers = 1 << 1,

        [Description("Score was missing required modifiers.")]
        MissingModifiers = 1 << 2,

        [Description("Score had too much pause time.")]
        PausedTooMuch = 1 << 3,

        [Description("Score was not a full combo when one was required.")]
        NoFullCombo = 1 << 4,

        [Description("Score was missing trackers.")]
        MissingTrackers = 1 << 5
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