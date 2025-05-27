using GuildSaber.Api.Endpoints.Internal;
using GuildSaber.Api.Extensions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Endpoints.Guilds.Members;

public class Endpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("guilds/{guildId}/members")
            .WithTag("Guilds.Members", description: "Endpoints for managing guild members by guild id.");

        group.MapGet("/", async (Guild.GuildId guildId, ServerDbContext dbContext)
            => await dbContext.Members.Where(x => x.GuildId == guildId).ToListAsync()
        );

        var withPlayerGroup = group.MapGroup("{playerId}")
            .WithSummary("Endpoints for managing a specific guild member.")
            .WithDescription("Endpoints for managing a specific guild member by player id.");

        withPlayerGroup.MapGet("/", GetMemberAsync)
            .WithName("GetMember")
            .WithSummary("Get a member of the guild by player id.")
            .WithDescription("Get a member of the guild by player id.");

        var atMeGroup = group.MapGroup("/@me")
            .WithSummary("Manage current user's guild membership")
            .WithDescription("Endpoints for managing the current user's guild membership by guild id.");

        atMeGroup.MapGet("/", (Guild.GuildId guildId) => TypedResults.Ok(guildId));
    }

    private static async Task<Results<Ok<Member>, NotFound>> GetMemberAsync
        (Guild.GuildId guildId, Player.PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId)switch
        {
            { } member => TypedResults.Ok(member),
            null => TypedResults.NotFound()
        };
}