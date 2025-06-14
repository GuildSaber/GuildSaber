using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Members.MemberService;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.Guilds.Members;

public class MemberEndpoints : IEndPoints
{
    public const string GetMemberName = "GetMember";

    public static void AddServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<MemberService>();

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("guilds/{guildId}/members")
            .WithTag("Guilds.Members", description: "Endpoints for managing guild members by guild id.");

        group.MapGet("/", GetMembersAsync)
            .WithName("GetMembers")
            .WithSummary("Get all members of a guild.")
            .WithDescription("Get all members of a guild by guild id.");

        group.MapPost("/", CurrentPlayerJoinGuildAsync)
            .WithName("CurrentPlayerJoinGuild")
            .WithSummary("Join a guild as the current player.")
            .WithDescription("Join a guild as the current player using their player id from claims.")
            .Produces<MemberResponses.Member>()
            .Produces<MemberResponses.Member>(StatusCodes.Status202Accepted)
            .Produces<MemberResponses.Member>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireAuthorization();

        var withPlayerGroup = group.MapGroup("/{playerId}")
            .WithSummary("Endpoints for managing a specific guild member.")
            .WithDescription("Endpoints for managing a specific guild member by player id.");

        withPlayerGroup.MapGet("/", GetMemberAsync)
            .WithName(GetMemberName)
            .WithSummary("Get a member of a guild")
            .WithDescription("Get a member of a guild by player id.");

        withPlayerGroup.MapPost("/", JoinGuildAsync)
            .WithName("JoinGuild")
            .WithSummary("Join a guild")
            .WithDescription("Join a guild by player id.")
            .Produces<MemberResponses.Member>()
            .Produces<MemberResponses.Member>(StatusCodes.Status202Accepted)
            .Produces<MemberResponses.Member>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireManager();
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

    /// <inheritdoc cref="JoinGuildAsync" />
    /// <remarks>
    /// Uses the player ID from the current authenticated user's claims.
    /// Returns 401 Unauthorized if no valid player ID is found in the claims.
    /// </remarks>
    private static async Task<IResult> CurrentPlayerJoinGuildAsync(
        Guild.GuildId guildId, ServerDbContext dbContext, MemberService memberService,
        LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var playerId = httpContext.User.GetPlayerId();
        if (playerId is null) return Results.Unauthorized();

        return await JoinGuildAsync(guildId, playerId.Value, memberService, linkGenerator, httpContext);
    }

    /// <inheritdoc cref="MemberService.JoinGuildAsync" />
    /// <remarks>
    /// This endpoint:
    /// - Returns 200 OK with member details when player joins successfully
    /// - Returns 202 Accepted with member details when join request submitted for approval
    /// - Returns 409 Conflict with member details when player is already a member
    /// - Returns 400 Bad Request with validation details when requirements aren't met
    /// - Returns 404 Not Found with problem details when guild, player or BeatLeader profile is not found
    /// - Returns 500 Internal Server Error when database persistence fails (e.g., due to a server error)
    /// </remarks>
    private static async Task<IResult> JoinGuildAsync(
        Guild.GuildId guildId, Player.PlayerId playerId,
        MemberService memberService, LinkGenerator linkGenerator, HttpContext httpContext)
        => await memberService.JoinGuildAsync(guildId, playerId) switch
        {
            JoinResponse.Success(var member) => TypedResults
                .Ok(member.Map()),
            JoinResponse.Requested(var member) => TypedResults
                .Accepted(linkGenerator.GetUriByName(httpContext, GetMemberName, new { guildId, playerId }),
                    member.Map()),
            JoinResponse.AlreadyMember(var member) => TypedResults
                .Conflict(member.Map()),
            JoinResponse.GuildNotFound => TypedResults.Problem(
                $"Guild with ID {guildId} not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Guild Not Found"
            ),
            JoinResponse.PlayerNotFound => TypedResults.Problem(
                $"Player with ID {playerId} not found.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Player Not Found"
            ),
            JoinResponse.BeatLeaderProfileNotFound(var blId, var error) => TypedResults.Problem(
                $"BeatLeader profile with ID {blId} not found. Error: {error}",
                statusCode: StatusCodes.Status404NotFound,
                title: "BeatLeader Profile Not Found"),
            JoinResponse.RequirementsFailure(var errors) => TypedResults.ValidationProblem(
                errors: errors,
                detail: "Failed to meet one or more join requirements",
                title: "Join Requirements Not Met"),
            JoinResponse.PersistenceError(var error) => TypedResults.Problem(
                error,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error saving member"),
            _ => throw new ArgumentOutOfRangeException(nameof(memberService.JoinGuildAsync), "Unexpected response type")
        };
}