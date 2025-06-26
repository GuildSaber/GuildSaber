namespace GuildSaber.Api.Features.Guilds.GuildStats;

public static class GuildStatsResponses
{
    public readonly record struct GuildStatsResponse(
        uint GuildId,
        int MemberCount,
        int RankedScoreCount
    );
}