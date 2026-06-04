using GuildSaber.Api.Features.Players.Http;

namespace GuildSaber.Api.Features.Leaderboards.Http;

public static class LeaderboardResponses
{
    public readonly record struct MemberPointStat(
        PlayerResponses.Player Player,
        float Points,
        int PassCount
    );
}