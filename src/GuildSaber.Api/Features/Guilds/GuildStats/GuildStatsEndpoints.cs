using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.GuildStats.GuildStatsResponses;

namespace GuildSaber.Api.Features.Guilds.GuildStats;

public class GuildStatsEndpoints : IEndpoints
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

    private static async Task<Results<Ok<GuildStatsResponse>, NotFound>> GetGuildStatsAsync
        (GuildId guildId, ServerDbContext dbContext) => await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .Select(GuildStatsMappers.MapGuildStatsExpression)
            .Cast<GuildStatsResponse?>()
            .FirstOrDefaultAsync() switch
        {
            null => TypedResults.NotFound(),
            var stats => TypedResults.Ok(stats.Value)
        };
}