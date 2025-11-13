using GuildSaber.Api.Features.Players;

namespace GuildSaber.Api.Features.Leaderboards;

public static class LeaderboardResponses
{
    public record PlayerWithPoints(PlayerResponses.Player Player, float Points);
}