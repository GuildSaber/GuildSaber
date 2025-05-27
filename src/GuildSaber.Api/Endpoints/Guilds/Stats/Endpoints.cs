using GuildSaber.Api.Endpoints.Internal;
using GuildSaber.Api.Extensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Endpoints.Guilds.Stats;

public class Endpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds/{guildId}/stats")
            .WithTag("Guilds.Stats", description: "Endpoints for exploring guild statistics by guild id.");

        group.MapGet("/", GetGuildStatsAsync)
            .WithName("GetGuildStats")
            .WithSummary("Get the statistics of the guild by guild id.")
            .WithDescription("Get the statistics of the guild by guild id.");
    }

    private static async Task<Results<Ok<Responses.GuildStatsResponse>, NotFound>> GetGuildStatsAsync
        (Guild.GuildId guildId, ServerDbContext dbContext)
        => await dbContext.Guilds
                .Where(x => x.Id == guildId)
                .Select(x => new Responses.GuildStatsResponse
                {
                    GuildId = x.Id,
                    MemberCount = x.Members.Count,
                    RankedScoreCount = dbContext.RankedScores
                        .Count(y => y.GuildId == guildId)
                }).FirstOrDefaultAsync() switch
            {
                var response when response.GuildId == new Guild.GuildId(0) => TypedResults.NotFound(),
                var stats => TypedResults.Ok(stats)
            };
}