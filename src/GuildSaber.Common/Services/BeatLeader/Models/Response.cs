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

public class PlayerScoreStats
{
    public int Id { get; set; }

    public long TotalScore { get; set; }
    public long TotalUnrankedScore { get; set; }
    public long TotalRankedScore { get; set; }

    public int LastScoreTime { get; set; }
    public int LastUnrankedScoreTime { get; set; }
    public int LastRankedScoreTime { get; set; }

    public float AverageRankedAccuracy { get; set; }
    public float AverageWeightedRankedAccuracy { get; set; }
    public float AverageUnrankedAccuracy { get; set; }
    public float AverageAccuracy { get; set; }

    public float MedianRankedAccuracy { get; set; }
    public float MedianAccuracy { get; set; }

    public float TopRankedAccuracy { get; set; }
    public float TopUnrankedAccuracy { get; set; }
    public float TopAccuracy { get; set; }

    public float TopPp { get; set; }

    public int RankedPlayCount { get; set; }
    public int UnrankedPlayCount { get; set; }
    public int TotalPlayCount { get; set; }

    public float AverageRankedRank { get; set; }
    public float AverageWeightedRankedRank { get; set; }
    public float AverageUnrankedRank { get; set; }
    public float AverageRank { get; set; }

    public string TopPlatform { get; set; } = "";
    public HMD TopHMD { get; set; }
}

public interface IWithPlayerScoreStats
{
    public PlayerScoreStats Stats { get; init; }
}

public class PlayerResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Platform { get; init; }
    public required string Avatar { get; init; }
    public required string Country { get; init; }
    public required string? Alias { get; init; }

    public required bool Bot { get; init; }

    public required float Pp { get; init; }
    public required int Rank { get; init; }
    public required int CountryRank { get; init; }
}

public class PlayerResponseWithStats : PlayerResponse, IWithPlayerScoreStats

{
    public required PlayerScoreStats Stats { get; init; }
}

public class PlayerResponseFull : PlayerResponse
{
    public required bool Banned { get; init; }
    public required bool Inactive { get; init; }
    public required Ban? BanDescription { get; init; }
    public required string ExternalProfileUrl { get; init; } = "";
    public required LinkResponse? LinkedIds { get; init; }
}

public class PlayerResponseFullWithStats : PlayerResponseFull, IWithPlayerScoreStats
{
    public required PlayerScoreStats Stats { get; init; }
}

public class Ban
{
    public required int Id { get; init; }
    public required string PlayerId { get; init; }
    public required string BannedBy { get; init; }
    public required string BanReason { get; init; }
    public required int TimeSet { get; init; }
    public required int Duration { get; init; }
}

public class LinkResponse
{
    public required int? QuestId { get; init; }
    public required string? SteamId { get; init; }
    public required string? OculusPCId { get; init; }
}