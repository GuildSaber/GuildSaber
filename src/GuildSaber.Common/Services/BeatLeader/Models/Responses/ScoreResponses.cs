namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public record CompactScore
{
    public required int? Id { get; init; }
    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required string Modifiers { get; init; }
    public required bool FullCombo { get; init; }
    public required int MaxCombo { get; init; }
    public required int MissedNotes { get; init; }
    public required int BadCuts { get; init; }
    public required HMD Hmd { get; init; }
    public required ControllerEnum Controller { get; init; }
    public required float Accuracy { get; init; }
    public required float? Pp { get; init; }

    public required int EpochTime { get; init; }
}

public record CompactScoreResponse
{
    public required CompactScore Score { get; init; }
    public required CompactLeaderboard Leaderboard { get; init; }
}

public record ScoreResponse : IProcessedScore, IWithPP, IWithPlayer, IWithScoreImprovement, IWithReplaysWatched
{
    public required int Id { get; init; }
    public required string PlayerId { get; init; }
    public required string LeaderboardId { get; init; }
    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required int? MaxStreak { get; init; }
    public required int Rank { get; init; }
    public required string Country { get; init; }
    public required string Replay { get; init; }
    public required string Modifiers { get; init; }
    public required int BadCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int BombCuts { get; init; }
    public required int WallsHit { get; init; }
    public required int Pauses { get; init; }
    public required bool FullCombo { get; init; }
    public required string Platform { get; init; }
    public required int MaxCombo { get; init; }
    public required HMD Hmd { get; init; }
    public required ControllerEnum Controller { get; init; }
    public required string TimeSet { get; init; }
    public required int TimePost { get; init; }
    public required ReplayOffsets Offsets { get; init; }
    public required float FcAccuracy { get; init; }

    public required PlayerResponse? Player { get; init; }
    public required float Pp { get; init; }
    public required float BonusPp { get; init; }
    public required float PassPP { get; init; }
    public required float AccPP { get; init; }
    public required float TechPP { get; init; }
    public required float FcPp { get; init; }
    public required float Weight { get; init; }
    public required int ReplaysWatched { get; init; }
    public required int PlayCount { get; init; }
    public required int LastTryTime { get; init; }
    public required ScoreImprovement? ScoreImprovement { get; init; }
}

public record ReplayOffsets
{
    public required int Id { get; init; }
    public required int Frames { get; init; }
    public required int Notes { get; init; }
    public required int Walls { get; init; }
    public required int Heights { get; init; }
    public required int Pauses { get; init; }
    public required int SaberOffsets { get; init; }
    public required int CustomData { get; init; }
}

public record ScoreResponseWithAcc : ScoreResponse, IWithAcc
{
    public required float AccLeft { get; init; }
    public required float AccRight { get; init; }
}

public record ScoreResponseWithHeadsets : ScoreResponse, IWithHeadsets
{
    public required string? HeadsetName { get; init; }
    public required string? ControllerName { get; init; }
}

public record ScoreResponseWithMyScore : ScoreResponseWithAcc, IWithMyScore
{
    public required LeaderboardContexts ValidContexts { get; init; }
    public required ScoreResponseWithAcc? MyScore { get; init; }
    public required float Experience { get; init; }
}

public enum EndType
{
    Unknown = 0,
    Clear = 1,
    Fail = 2,
    Restart = 3,
    Quit = 4,
    Practice = 5
}

public record AttemptResponseWithMyScore : ScoreResponseWithAcc, IWithMyScore
{
    public required EndType EndType { get; init; }
    public required int AttemptsCount { get; init; }
    public required float Time { get; init; }
    public required float StartTime { get; init; }
    public required float Speed { get; init; }

    public required CompactLeaderboardResponse Leaderboard { get; init; }
    public required ScoreResponseWithAcc? MyScore { get; init; }
    public required float Experience { get; init; }
}

public record ScoreContextExtensionResponse
{
    public required int Id { get; init; }
    public required string PlayerId { get; init; }

    public required float Weight { get; init; }
    public required int Rank { get; init; }
    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required float Pp { get; init; }
    public required float PassPP { get; init; }
    public required float AccPP { get; init; }
    public required float TechPP { get; init; }
    public required float BonusPp { get; init; }
    public required string? Modifiers { get; init; }

    public required LeaderboardContexts Context { get; init; }
    public required ScoreImprovement? ScoreImprovement { get; init; }
}

public record CommonScores
{
    public required CompactLeaderboardResponse Leaderboard { get; init; }
    public required List<ScoreResponse> Scores { get; init; }
}

[Flags]
public enum LeaderboardContexts
{
    None = 0,
    General = 1 << 1,
    NoMods = 1 << 2,
    NoPause = 1 << 3,
    Golf = 1 << 4,
    SCPM = 1 << 5,
    Speedrun = 1 << 6,
    SpeedrunBackup = 1 << 7,
    Funny = 1 << 8
}

public record ScoreResponseWithMyScoreAndContexts : ScoreResponseWithMyScore, IWithScoreContext
{
    public required ICollection<ScoreContextExtensionResponse> ContextExtensions { get; init; }
}

public record ScoreImprovement
{
    public required int Id { get; init; }
    public required string TimeSet { get; init; } = "";
    public required int Score { get; init; }
    public required float Accuracy { get; init; }
    public required float Pp { get; init; }
    public required float BonusPp { get; init; }
    public required int Rank { get; init; }
    public required float AccRight { get; init; }
    public required float AccLeft { get; init; }

    public required float AverageRankedAccuracy { get; init; }
    public required float TotalPp { get; init; }
    public required int TotalRank { get; init; }

    public required int BadCuts { get; init; }
    public required int MissedNotes { get; init; }
    public required int BombCuts { get; init; }
    public required int WallsHit { get; init; }
    public required int Pauses { get; init; }

    public required string Modifiers { get; init; }
}

public record ScoreSongResponse
{
    public required string Id { get; init; }
    public required string Hash { get; init; }
    public required string Cover { get; init; }
    public required string Name { get; init; }
    public required string? SubName { get; init; }
    public required string Author { get; init; }
    public required string Mapper { get; init; }
    public required string DownloadUrl { get; init; }
}

public record ScoreResponseWithDifficulty : ScoreResponse
{
    public required DifficultyDescription Difficulty { get; init; }
    public required ScoreSongResponse Song { get; init; }
    public required LeaderboardContexts ValidContexts { get; init; }
    public required ICollection<ScoreContextExtensionResponse> ContextExtensions { get; init; }
}

public record SaverScoreResponse
{
    public required int Id { get; init; }
    public required int BaseScore { get; init; }
    public required int ModifiedScore { get; init; }
    public required float Accuracy { get; init; }
    public required float Pp { get; init; }
    public required int Rank { get; init; }
    public required string Modifiers { get; init; }
    public required string LeaderboardId { get; init; }
    public required string TimeSet { get; init; }
    public required int TimePost { get; init; }
    public required string Player { get; init; }
}

public record SaverContainerResponse
{
    public required string LeaderboardId { get; init; }
    public required bool Ranked { get; init; }
}