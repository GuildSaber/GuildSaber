using System.ComponentModel;
using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Api.Features.RankedMaps.Http;
using static GuildSaber.Api.Features.Scores.Http.ScoreResponses;

namespace GuildSaber.Api.Features.RankedScores.Http;

public static class RankedScoreResponses
{
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
        RankedMapId RankedMapId,
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
            RankedMapId RankedMapId,
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
            RankedMapId RankedMapId,
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
            RankedMapId RankedMapId,
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
            RankedMapId RankedMapId,
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
            RankedMapId RankedMapId,
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
}