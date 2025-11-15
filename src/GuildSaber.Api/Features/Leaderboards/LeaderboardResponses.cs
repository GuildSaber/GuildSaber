using GuildSaber.Api.Features.Players;

namespace GuildSaber.Api.Features.Leaderboards;

public static class LeaderboardResponses
{
    public readonly record struct MemberStat(
        PlayerResponses.Player Player,
        float Points,
        float Xp,
        int PassCount,
        int? LevelId,
        int? NextLevelId
    );
}