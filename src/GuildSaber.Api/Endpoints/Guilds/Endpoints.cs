using GuildSaber.Api.Endpoints.Internal;
using GuildSaber.Api.Extensions;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Endpoints.Guilds;

public class Endpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds")
            .WithTag("Guilds", description: "Endpoints for managing guilds in the server.");

        group.MapGet("/", (ServerDbContext dbContext) => dbContext.Guilds.ToListAsync())
            .WithName("GetGuilds")
            .WithSummary("Get all guilds.")
            .WithDescription("Get all guilds in the server.");

        // get
        // delete
        // patch
        // post
    }
}