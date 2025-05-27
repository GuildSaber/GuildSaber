using GuildSaber.Database.Models.Server.Guilds;

namespace GuildSaber.Api.Endpoints.Guilds.Stats;

public static class Responses
{
    public readonly record struct GuildStatsResponse(
        Guild.GuildId GuildId,
        int MemberCount,
        int RankedScoreCount
    );
}