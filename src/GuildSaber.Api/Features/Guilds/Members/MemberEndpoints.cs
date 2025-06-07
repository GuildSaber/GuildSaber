using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members;

public class MemberEndpoints : IEndPoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("guilds/{guildId}/members")
            .WithTag("Guilds.Members", description: "Endpoints for managing guild members by guild id.");

        group.MapGet("/", GetMembersAsync)
            .WithName("GetMembers")
            .WithSummary("Get all members of a guild.")
            .WithDescription("Get all members of a guild by guild id.");

        var withPlayerGroup = group.MapGroup("/{playerId}")
            .WithSummary("Endpoints for managing a specific guild member.")
            .WithDescription("Endpoints for managing a specific guild member by player id.");

        withPlayerGroup.MapGet("/", GetMemberAsync)
            .WithName("GetMember")
            .WithSummary("Get a member of a guild")
            .WithDescription("Get a member of a guild by player id.");
    }

    private static async Task<Results<Ok<Member>, NotFound>> GetMemberAsync(
        Guild.GuildId guildId, Player.PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId)switch
        {
            { } member => TypedResults.Ok(member),
            null => TypedResults.NotFound()
        };

    private static async Task<Results<Ok<PagedList<Member>>, ProblemHttpResult>> GetMembersAsync(
        Guild.GuildId guildId,
        ServerDbContext dbContext,
        [Range(0, int.MaxValue)] int page = 1,
        [Range(0, 100)] int pageSize = 10,
        MemberRequests.EMemberSorters sortBy = MemberRequests.EMemberSorters.CreatedAt,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await PagedList<Member>.CreateAsync(
            ApplySortOrder(dbContext.Members.Where(x => x.GuildId == guildId), sortBy, order), page, pageSize));

    private static IQueryable<Member> ApplySortOrder(
        IQueryable<Member> query, MemberRequests.EMemberSorters sortBy, EOrder order) => sortBy switch
    {
        MemberRequests.EMemberSorters.PlayerId => query.OrderBy(order, x => x.PlayerId),
        MemberRequests.EMemberSorters.CreatedAt => query.OrderBy(order, x => x.CreatedAt)
            .ThenBy(order, x => x.PlayerId),
        MemberRequests.EMemberSorters.JoinState => query.OrderBy(order, x => x.JoinState)
            .ThenBy(order, x => x.PlayerId),
        MemberRequests.EMemberSorters.EditedAt => query.OrderBy(order, x => x.EditedAt)
            .ThenBy(order, x => x.PlayerId),
        MemberRequests.EMemberSorters.Permissions => query.OrderBy(order, x => x.Permissions)
            .ThenBy(order, x => x.EditedAt)
            .ThenBy(order, x => x.PlayerId),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}