using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds;

namespace GuildSaber.Api.Features.Guilds.GuildStats;

public static class GuildStatsMappers
{
    public static Expression<Func<Guild, GuildStatsResponses.GuildStatsResponse>> MapGuildStatsExpression
        => self => new GuildStatsResponses.GuildStatsResponse(
            self.Id,
            self.Members.Count,
            self.RankedScores.Count
        );
}