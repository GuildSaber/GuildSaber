using GuildSaber.Api.Endpoints.Internal;
using GuildSaber.Database.Models.Server.Guilds;

namespace GuildSaber.Api.Endpoints.Guilds.Stats;

public class Endpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds/{guildId}/stats")
            .WithTags("Guilds.Stats")
            .WithSummary("Endpoints for managing guild statistics.")
            .WithDescription("Endpoints for managing guild statistics by guild id.");

        group.MapGet("/", (Guild.GuildId guildId) => TypedResults.Ok(guildId))
            .WithName("GetGuildStats")
            .WithSummary("Get the statistics of the guild by guild id.")
            .WithDescription("Get the statistics of the guild by guild id.");
    }
}