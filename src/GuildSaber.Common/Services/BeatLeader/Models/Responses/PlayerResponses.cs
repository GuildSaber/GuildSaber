namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public class PlayerScoreStats
{
    public required int Id { get; init; }

    public required long TotalScore { get; init; }
    public required long TotalUnrankedScore { get; init; }
    public required long TotalRankedScore { get; init; }

    public required int FirstScoreTime { get; init; }
    public required int FirstUnrankedScoreTime { get; init; }
    public required int FirstRankedScoreTime { get; init; }

    public required int LastScoreTime { get; init; }
    public required int LastUnrankedScoreTime { get; init; }
    public required int LastRankedScoreTime { get; init; }

    public required float AverageRankedAccuracy { get; init; }
    public required float AverageWeightedRankedAccuracy { get; init; }
    public required float AverageUnrankedAccuracy { get; init; }
    public required float AverageAccuracy { get; init; }

    public required float MedianRankedAccuracy { get; init; }
    public required float MedianAccuracy { get; init; }

    public required float TopRankedAccuracy { get; init; }
    public required float TopUnrankedAccuracy { get; init; }
    public required float TopAccuracy { get; init; }

    public required float TopPp { get; init; }

    public required int RankedPlayCount { get; init; }
    public required int UnrankedPlayCount { get; init; }
    public required int TotalPlayCount { get; init; }

    public required float AverageRankedRank { get; init; }
    public required float AverageWeightedRankedRank { get; init; }
    public required float AverageUnrankedRank { get; init; }
    public required float AverageRank { get; init; }

    public required string TopPlatform { get; init; } = "";
    public required HMD TopHMD { get; init; }
}

public interface IWithPlayerScoreStats
{
    public PlayerScoreStats ScoreStats { get; init; }
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
    public required PlayerScoreStats ScoreStats { get; init; }
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
    public required PlayerScoreStats ScoreStats { get; init; }
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

public class PlayerWithBanHistory
{
    public required string Id { get; init; }
    public required string Name { get; init; } = "";
    public required string Platform { get; init; } = "";
    public required string Avatar { get; init; } = "";
    public required string Country { get; init; } = "not set";
    public required string? Alias { get; init; }

    public required bool Bot { get; init; }
    public required bool Banned { get; init; }

    public required float Pp { get; init; }
    public required int Rank { get; init; }
    public required int CountryRank { get; init; }
    public required string Role { get; init; }

    public required List<Ban> Bans { get; init; }
}