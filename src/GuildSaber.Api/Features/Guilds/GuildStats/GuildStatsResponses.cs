using GuildSaber.Database.Models.Server.Guilds;

namespace GuildSaber.Api.Features.Guilds.GuildStats;

public static class GuildStatsResponses
{
    public readonly record struct GuildStatsResponse(
        Guild.GuildId GuildId,
        int MemberCount,
        int RankedScoreCount
    );
}