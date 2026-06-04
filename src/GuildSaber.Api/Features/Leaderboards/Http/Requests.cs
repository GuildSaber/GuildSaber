namespace GuildSaber.Api.Features.Leaderboards.Http;

public static class LeaderboardRequests
{
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