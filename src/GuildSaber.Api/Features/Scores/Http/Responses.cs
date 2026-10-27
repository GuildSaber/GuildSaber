using System.Text.Json.Serialization;
using GuildSaber.Api.Features.Hardware.Http;
using GuildSaber.Api.Features.RankedMaps.Http;

namespace GuildSaber.Api.Features.Scores.Http;

public static class ScoreResponses
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(BeatLeaderScore), "BeatLeader")]
    [JsonDerivedType(typeof(ScoreSaberScore), "ScoreSaber")]
    public abstract record Score(
        ScoreId Id,
        long SongDifficultyId,
        int BaseScore,
        RankedMapRequests.EModifiers Modifiers,
        DateTimeOffset SetAt,
        int? MaxCombo,
        bool IsFullCombo,
        int MissedNotes,
        int BadCuts,
        Devices Devices,
        PauseStatistics? PauseStatistics,
        bool HasStatistics)
    {
        public sealed record BeatLeaderScore(
            ScoreId Id,
            long SongDifficultyId,
            int BaseScore,
            RankedMapRequests.EModifiers Modifiers,
            DateTimeOffset SetAt,
            int? MaxCombo,
            bool IsFullCombo,
            int MissedNotes,
            int BadCuts,
            Devices Devices,
            PauseStatistics? PauseStatistics,
            bool HasStatistics,
            BeatLeaderScoreId? BeatLeaderScoreId
        ) : Score(Id, SongDifficultyId, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts,
            Devices, PauseStatistics, HasStatistics);

        public sealed record ScoreSaberScore(
            ScoreId Id,
            long SongDifficultyId,
            int BaseScore,
            RankedMapRequests.EModifiers Modifiers,
            DateTimeOffset SetAt,
            int? MaxCombo,
            bool IsFullCombo,
            int MissedNotes,
            int BadCuts,
            Devices Devices,
            PauseStatistics? PauseStatistics,
            bool HasStatistics,
            ScoreSaberScoreId ScoreSaberScoreId
        ) : Score(Id, SongDifficultyId, BaseScore, Modifiers, SetAt, MaxCombo, IsFullCombo, MissedNotes, BadCuts,
            Devices, PauseStatistics, HasStatistics);
    }

    public record Devices(
        HardwareResponses.Hmd? Hmd,
        HardwareResponses.Controller? ControllerLeft,
        HardwareResponses.Controller? ControllerRight);

    public record ScoreStatistics(
        WinTracker WinTracker,
        HitTracker HitTracker,
        AccuracyTracker AccuracyTracker,
        ScoreGraphTracker ScoreGraphTracker
    );

    public record WinTracker(
        bool IsWin,
        float EndTime,
        PauseStatistics? PauseStatistics,
        float? JumpDistance,
        float AverageHeight,
        int TotalScore,
        int MaxScore,
        // ReSharper disable once MemberHidesStaticFromOuterClass
        AverageHeadPosition? AverageHeadPosition
    );

    public record PauseStatistics(int PauseCount, float TotalPauseDuration);

    public record HitTracker(
        int? Max115Streak,
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

    public readonly record struct AverageHeadPosition(float X, float Y, float Z);
}