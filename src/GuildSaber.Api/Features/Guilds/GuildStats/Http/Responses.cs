namespace GuildSaber.Api.Features.Guilds.GuildStats.Http;

public static class GuildStatsResponses
{
    public readonly record struct GuildStatsResponse(
        int GuildId,
        int MemberCount,
        int RankedScoreCount
    );
}