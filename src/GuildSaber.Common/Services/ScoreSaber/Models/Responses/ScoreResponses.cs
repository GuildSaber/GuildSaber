using System.Text.Json.Serialization;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Common.Services.ScoreSaber.Models.Responses;

public record ScoreCollection<TScore>(
    [property: JsonRequired] TScore[] Data,
    [property: JsonRequired] PaginationMetadata Metadata
);

public record PaginationMetadata(
    [property: JsonRequired] int Page,
    [property: JsonRequired] int ItemsPerPage,
    [property: JsonRequired] int TotalItems,
    [property: JsonRequired] int TotalPages
);

public record PlayerScore(
    [property: JsonRequired] Score Score,
    [property: JsonRequired] Leaderboard Leaderboard
);

public record ScoreDetails(
    [property: JsonRequired] Leaderboard Leaderboard,
    [property: JsonRequired] Score Score,
    [property: JsonRequired] ScoreStats? ScoreStats
);

public record Device(
    [property: JsonRequired] string? Hmd,
    [property: JsonRequired] string? ControllerLeft,
    [property: JsonRequired] string? ControllerRight
);

public record Player(
    [property: JsonRequired] ScoreSaberId Id,
    [property: JsonRequired] string Name,
    [property: JsonRequired] string PlayerNameInGame,
    [property: JsonRequired] string Country,
    [property: JsonRequired] string? Role,
    [property: JsonRequired] string Avatar,
    [property: JsonRequired] long AvatarVersion,
    [property: JsonRequired] int Permissions
);

public record Leaderboard(
    [property: JsonRequired] DateTimeOffset CreatedAt,
    [property: JsonRequired] int DailyScores,
    [property: JsonRequired] Difficulty Difficulty,
    [property: JsonRequired] SSLeaderboardId Id,
    [property: JsonRequired] Map Map,
    [property: JsonRequired] int MaxScore,
    [property: JsonRequired] Realm Realm,
    [property: JsonRequired] int TotalScores,
    int? LegacyMaxScore = null
);

/// <remarks>Not using SongHash strong type because ScoreSaber sends "_ostFoo" as song hash for OSTs.</remarks>
public record Map(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Hash,
    [property: JsonRequired] BeatSaverKey? Bsid,
    [property: JsonRequired] string SongName,
    [property: JsonRequired] string SongSubName,
    [property: JsonRequired] string SongAuthorName,
    [property: JsonRequired] string LevelAuthorName,
    [property: JsonRequired] double Bpm,
    [property: JsonRequired] string CoverUrl,
    [property: JsonRequired] bool Verified
);

public record Realm(
    [property: JsonRequired] int RealmId,
    [property: JsonRequired] string RealmName,
    [property: JsonRequired] LeaderboardStatus LeaderboardStatus,
    [property: JsonRequired] bool PositiveModifiers,
    [property: JsonRequired] double Stars,
    [property: JsonRequired] DateTimeOffset? RankedAt,
    [property: JsonRequired] DateTimeOffset? QualifiedAt,
    [property: JsonRequired] DateTimeOffset? LovedAt,
    string? ModifierValues = null
);

[JsonConverter(typeof(JsonStringEnumConverter<LeaderboardStatus>))]
public enum LeaderboardStatus
{
    [JsonStringEnumMemberName("UNRANKED")] Unranked = 0,
    [JsonStringEnumMemberName("RANKED")] Ranked = 1,
    [JsonStringEnumMemberName("QUALIFIED")] Qualified = 2,
    [JsonStringEnumMemberName("LOVED")] Loved = 3
}

public record Difficulty(
    [property: JsonRequired] SSLeaderboardId Id,
    [property: JsonRequired, JsonPropertyName("difficulty")] EDifficulty EDifficulty,
    [property: JsonRequired] SSGameMode GameMode,
    [property: JsonRequired] string RawDifficulty
);

public record Score(
    [property: JsonRequired] double Accuracy,
    [property: JsonRequired] int BadCuts,
    [property: JsonRequired] DateTimeOffset CreatedAt,
    [property: JsonRequired] Device? Device,
    [property: JsonRequired] bool FullCombo,
    [property: JsonRequired] bool HasReplay,
    [property: JsonRequired] ScoreSaberScoreId Id,
    [property: JsonRequired, JsonPropertyName("legacyHmdId")] LegacyHMD? LegacyHmd,
    [property: JsonRequired] int MaxCombo,
    [property: JsonRequired] int MissedNotes,
    [property: JsonRequired] int ModifiedScore,
    [property: JsonRequired] string[] Mods,
    [property: JsonRequired] bool PersonalBest,
    [property: JsonRequired] Player Player,
    [property: JsonRequired] PlayOutcome PlayOutcome,
    [property: JsonRequired] double? PlayOutcomeTime,
    [property: JsonRequired] double Pp,
    [property: JsonRequired] int Rank,
    [property: JsonRequired] int UnmodifiedScore,
    [property: JsonRequired] string? Version,
    [property: JsonRequired] double Weight,
    bool HasHistory = false,
    int ReplayViewCount = 0
);

[JsonConverter(typeof(JsonStringEnumConverter<PlayOutcome>))]
public enum PlayOutcome
{
    [JsonStringEnumMemberName("CLEAR")] Clear = 0,
    [JsonStringEnumMemberName("FAIL")] Fail = 1,
    [JsonStringEnumMemberName("QUIT")] Quit = 2,
    [JsonStringEnumMemberName("RESTART")] Restart = 3
}

public record ScoreStats(
    [property: JsonRequired] int TotalScore,
    [property: JsonRequired] int MaxScore,
    [property: JsonRequired] bool Passed,
    [property: JsonRequired] double FailTime,
    [property: JsonRequired] double EndTime,
    [property: JsonRequired] int MaxCombo,
    [property: JsonRequired] int? Max115Streak,
    [property: JsonRequired] double FcAcc,
    [property: JsonRequired] double AccLeft,
    [property: JsonRequired] double AccRight,
    [property: JsonRequired] double LeftPreswing,
    [property: JsonRequired] double RightPreswing,
    [property: JsonRequired] double LeftPostswing,
    [property: JsonRequired] double RightPostswing,
    [property: JsonRequired] double LeftTimeDependence,
    [property: JsonRequired] double RightTimeDependence,
    [property: JsonRequired] double LeftTiming,
    [property: JsonRequired] double RightTiming,
    [property: JsonRequired] double[] LeftAverageCut,
    [property: JsonRequired] double[] RightAverageCut,
    [property: JsonRequired] int LeftMiss,
    [property: JsonRequired] int RightMiss,
    [property: JsonRequired] int LeftBadCuts,
    [property: JsonRequired] int RightBadCuts,
    [property: JsonRequired] int LeftBombs,
    [property: JsonRequired] int RightBombs,
    [property: JsonRequired] double LeftSaberHitOffset,
    [property: JsonRequired] double RightSaberHitOffset,
    [property: JsonRequired] double NoteSpawnOffset,
    [property: JsonRequired] double AverageHeight,
    [property: JsonRequired] HeadPosition AverageHeadPosition,
    [property: JsonRequired] double[] ScoreGraph,
    [property: JsonRequired] GridCutDetails GridCutDetails,
    [property: JsonRequired] HandSummary[] HandSummary,
    [property: JsonRequired] AccuracyDistribution AccuracyDistribution,
    [property: JsonRequired] AccuracyTimeline AccuracyTimeline,
    [property: JsonRequired] UnderswingStats UnderswingStats,
    double? JumpDistance = null,
    int? PauseCount = null,
    double? PauseTotalDurationSeconds = null
);

public record HeadPosition(
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y,
    [property: JsonRequired] double Z
);

public record GridCutDetails(
    [property: JsonRequired] GridCell[] Grid,
    [property: JsonRequired] GridSummary[][] SummaryGrids
);

public record GridCell(
    [property: JsonRequired] int Count,
    [property: JsonRequired] double AvgScore,
    [property: JsonRequired] CutDetails[] Left,
    [property: JsonRequired] CutDetails[] Right
);

public record CutDetails(
    [property: JsonRequired] int Count,
    [property: JsonRequired] double AvgScore,
    [property: JsonRequired] double AvgCutAngle,
    [property: JsonRequired] double AvgCutOffset,
    [property: JsonRequired] double[] AvgCutScore
);

public record GridSummary(
    [property: JsonRequired] int Count,
    [property: JsonRequired] double AvgScore
);

public record HandSummary(
    [property: JsonRequired] string Label,
    [property: JsonRequired] HandStats Left,
    [property: JsonRequired] HandStats Right
);

public record HandStats(
    [property: JsonRequired] int Count,
    [property: JsonRequired] double AvgScore,
    [property: JsonRequired] double AvgTd
);

public record AccuracyDistribution(
    [property: JsonRequired] int LeftTotal,
    [property: JsonRequired] int RightTotal,
    [property: JsonRequired] double[] LeftCount,
    [property: JsonRequired] double[] RightCount,
    [property: JsonRequired] double?[] LeftTd,
    [property: JsonRequired] double?[] RightTd,
    [property: JsonRequired] double?[] TimingStdDev
);

public record AccuracyTimeline(
    [property: JsonRequired] double[] Times,
    [property: JsonRequired] AccuracyTimelineData Left,
    [property: JsonRequired] AccuracyTimelineData Right,
    [property: JsonRequired] AccuracyTimelineData Total
);

public record AccuracyTimelineData(
    [property: JsonRequired] double[] FullSwing,
    [property: JsonRequired] double[] Actual
);

public record UnderswingStats(
    [property: JsonRequired] int Count,
    [property: JsonRequired] int FcScore,
    [property: JsonRequired] int FullSwingScore,
    [property: JsonRequired] int FullSwingFcScore,
    [property: JsonRequired] int MaxCutScore,
    [property: JsonRequired] double FcAcc,
    [property: JsonRequired] double FullSwingAcc,
    [property: JsonRequired] double FullSwingFcAcc
);