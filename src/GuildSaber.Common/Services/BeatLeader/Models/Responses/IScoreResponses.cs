using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public interface IUnprocessedScore
{
    string PlayerId { get; init; }
    string LeaderboardId { get; init; }
    int BaseScore { get; init; }
    int ModifiedScore { get; init; }
    float Accuracy { get; init; }
    string Modifiers { get; init; }
    bool FullCombo { get; init; }
    int MissedNotes { get; init; }
    int BadCuts { get; init; }
    int BombCuts { get; init; }
    int WallsHit { get; init; }
    int Pauses { get; init; }
    string Platform { get; init; }
    HMD Hmd { get; init; }
    ControllerEnum Controller { get; init; }
    string TimeSet { get; init; }
    int TimePost { get; init; }
}

public interface IProcessedScore : IUnprocessedScore
{
    BeatLeaderScoreId Id { get; init; }
    int Rank { get; init; }
    string Country { get; init; }
    string Replay { get; init; }
    int? MaxStreak { get; init; }
    int MaxCombo { get; init; }
    public float FcAccuracy { get; init; }
    ReplayOffsets Offsets { get; init; }
}

public interface IWithAcc
{
    float AccLeft { get; init; }
    float AccRight { get; init; }
}

public interface IWithPP
{
    float BonusPp { get; init; }
    float PassPP { get; init; }
    float AccPP { get; init; }
    float TechPP { get; init; }
    float Pp { get; init; }
    float FcAccuracy { get; init; }
    float FcPp { get; init; }
    float Weight { get; init; }
}

public interface IWithReplaysWatched
{
    int ReplaysWatched { get; init; }
    int PlayCount { get; init; }
    int LastTryTime { get; init; }
}

public interface IWithHeadsets
{
    string? HeadsetName { get; init; }
    string? ControllerName { get; init; }
}

public interface IWithPlayer
{
    PlayerResponse? Player { get; init; }
}

public interface IWithScoreImprovement
{
    ScoreImprovement? ScoreImprovement { get; init; }
}

public interface IWithScoreContext
{
    LeaderboardContexts ValidContexts { get; init; }
    ICollection<ScoreContextExtensionResponse> ContextExtensions { get; init; }
}

public interface IWithMyScore
{
    ScoreResponseWithAcc? MyScore { get; init; }
    float Experience { get; init; }
}