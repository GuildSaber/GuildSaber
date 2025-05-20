namespace GuildSaber.Common.Services.BeatLeader.Models;

public class CompactScore
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

public class CompactLeaderboard
{
    public required string Id { get; init; }
    public required string SongHash { get; init; }
    public required string ModeName { get; init; }
    public required int Difficulty { get; init; }
}

public class CompactScoreResponse
{
    public required CompactScore Score { get; init; }
    public required CompactLeaderboard Leaderboard { get; init; }
}