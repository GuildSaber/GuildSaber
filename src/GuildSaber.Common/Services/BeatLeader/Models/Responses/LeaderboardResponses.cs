using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.Responses;

public class CompactLeaderboard
{
    public required string Id { get; init; }
    public required string SongHash { get; init; }
    public required string ModeName { get; init; }
    public required int Difficulty { get; init; }
}

public class LeaderboardsResponse<T>
{
    public required Song Song { get; init; }
    public required ICollection<T> Leaderboards { get; init; }
}

public class LeaderboardsResponse : LeaderboardsResponse<LeaderboardsInfoResponse>;
public class LeaderboardsResponseWithScores : LeaderboardsResponse<LeaderboardsInfoResponseWithScore>;

public class LeaderboardsInfoResponse
{
    public required BLLeaderboardId Id { get; init; }
    public required DifficultyResponse Difficulty { get; init; }
}

public class LeaderboardsInfoResponseWithScore : LeaderboardsInfoResponse
{
    public required ScoreResponseWithAcc? MyScore { get; init; }
}

public class CompactLeaderboardResponse
{
    public required BLLeaderboardId? Id { get; init; }
    public required CompactSongResponse? Song { get; init; }
    public required DifficultyResponse? Difficulty { get; init; }
}

public class LeaderboardResponse
{
    public required BLLeaderboardId? Id { get; init; }
    public required SongResponse? Song { get; init; }
    public required DifficultyResponse? Difficulty { get; init; }
    public required List<ScoreResponse>? Scores { get; init; }

    public required IEnumerable<LeaderboardGroupEntry>? LeaderboardGroup { get; init; }
    public required int Plays { get; init; }
}

public class LeaderboardGroupEntry
{
    public required BLLeaderboardId Id { get; init; }
    public required DifficultyStatus Status { get; init; }
    public required long Timestamp { get; init; }
}

public class LeaderboardInfoResponse
{
    public required string Id { get; init; }
    public required SongResponse Song { get; init; }
    public required DifficultyResponse Difficulty { get; init; }
    public required int Plays { get; init; }
    public required int Attempts { get; init; }
    public required int PositiveVotes { get; init; }
    public required int StarVotes { get; init; }
    public required int NegativeVotes { get; init; }
    public required float VoteStars { get; init; }
    public required bool ClanRankingContested { get; init; }
    public required ScoreResponseWithAcc? MyScore { get; init; }
}

public class TrendingLeaderboardInfoResponse : LeaderboardInfoResponse
{
    public required string Description { get; init; }
    public required string TrendingValue { get; init; }
    public required int ThisWeekPlays { get; init; }
    public required int TodayPlays { get; init; }
}