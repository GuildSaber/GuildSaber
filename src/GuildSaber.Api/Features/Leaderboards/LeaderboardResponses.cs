using GuildSaber.Api.Features.Players;

namespace GuildSaber.Api.Features.Leaderboards;

public static class LeaderboardResponses
{
    public readonly record struct MemberPointStat(
        PlayerResponses.Player Player,
        float Points,
        int PassCount
    );
}