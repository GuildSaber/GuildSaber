namespace GuildSaber.Api.Features.Leaderboards.Http;

public static class LeaderboardRequests
{
    /// <summary>
    /// Filters for querying leaderboards.
    /// </summary>
    /// <param name="Search">A search term to filter leaderboard entries by player username.</param>
    public record struct Filters(
        [FromQuery(Name = "search")] string? Search = null
    );

    public enum ERankedMapLeaderboardSorter
    {
        Points = 0,
        EffectiveScore = 1
    }

    public enum EMemberStatLeaderboardSorter
    {
        Points = 0,
        PassCount = 1
    }
}