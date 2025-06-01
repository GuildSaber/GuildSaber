using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds;

public class GuildEndpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds")
            .WithTag("Guilds", description: "Endpoints for managing guilds in the server.");

        group.MapGet("/", (ServerDbContext dbContext) => dbContext.Guilds.ToListAsync())
            .WithName("GetGuilds")
            .WithSummary("Get all guilds.")
            .WithDescription("Get all guilds in the server.");

        group.MapDelete("/{guildId}", DeleteGuildAsync)
            .WithName("DeleteGuild")
            .WithSummary("Delete a guild.")
            .WithDescription("Delete a guild by its ID.")
            .RequireGuildPermission(Member.EPermission.GuildSaberManager);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteGuildAsync(
        Guild.GuildId guildId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    // get
    // patch
    // post
}