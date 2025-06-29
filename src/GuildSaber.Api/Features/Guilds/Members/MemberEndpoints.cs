using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Members.MemberService;
using static GuildSaber.Api.Features.Guilds.Members.MemberResponses;
using ServerMember = GuildSaber.Database.Models.Server.Guilds.Members.Member;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.Guilds.Members;

public class MemberEndpoints : IEndpoints
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
            .Produces<Member>()
            .Produces<Member>(StatusCodes.Status202Accepted)
            .Produces<Member>(StatusCodes.Status409Conflict)
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
            .Produces<Member>()
            .Produces<Member>(StatusCodes.Status202Accepted)
            .Produces<Member>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .RequireManager();
    }

    private static async Task<Results<Ok<Member>, NotFound>> GetMemberAsync(
        GuildId guildId, PlayerId playerId, ServerDbContext dbContext)
        => await dbContext.Members.Where(x => x.GuildId == guildId && x.PlayerId == playerId)
                .Select(MemberMappers.MapMemberExpression)
                .Cast<Member?>()
                .FirstOrDefaultAsync() switch
            {
                { } member => TypedResults.Ok(member),
                null => TypedResults.NotFound()
            };

    private static async Task<Ok<PagedList<Member>>> GetMembersAsync(
        GuildId guildId, ServerDbContext dbContext,
        [Range(0, int.MaxValue)] int page = 1,
        [Range(0, 100)] int pageSize = 10,
        MemberRequests.EMemberSorters sortBy = MemberRequests.EMemberSorters.CreatedAt,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await dbContext.Members
            .Where(x => x.GuildId == guildId)
            .ApplySortOrder(sortBy, order)
            .Select(MemberMappers.MapMemberExpression)
            .ToPagedListAsync(page, pageSize));

    /// <inheritdoc cref="JoinGuildAsync" />
    /// <remarks>
    /// Uses the player ID from the current authenticated user's claims.
    /// Returns 401 Unauthorized if no valid player ID is found in the claims.
    /// </remarks>
    private static async Task<IResult> CurrentPlayerJoinGuildAsync(
        GuildId guildId, ServerDbContext dbContext, MemberService memberService,
        LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var playerId = httpContext.User.GetPlayerId();
        if (playerId is null) return Results.Unauthorized();

        return await JoinGuildAsync(guildId, playerId.Value, memberService, linkGenerator, httpContext);
    }

    /// <inheritdoc cref="MemberService.JoinGuildAsync" />
    /// <remarks>
    /// This endpoint:
    /// <list type="bullet">
    ///     <item>Returns 200 OK with member details when player joins successfully.</item>
    ///     <item>Returns 202 Accepted with member details when join request is submitted for approval.</item>
    ///     <item>Returns 409 Conflict with member details when player is already a member.</item>
    ///     <item>Returns 400 Bad Request with validation details when requirements aren't met.</item>
    ///     <item>Returns 404 Not Found with problem details when guild, player or BeatLeader profile is not found.</item>
    /// </list>
    /// </remarks>
    private static async Task<IResult> JoinGuildAsync(
        GuildId guildId, PlayerId playerId,
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
                statusCode: StatusCodes.Status404NotFound),
            JoinResponse.PlayerNotFound => TypedResults.Problem(
                $"Player with ID {playerId} not found.",
                statusCode: StatusCodes.Status404NotFound),
            JoinResponse.BeatLeaderProfileNotFound(var blId, var error) => TypedResults.Problem(
                $"BeatLeader profile with ID {blId} not found. Error: {error}",
                statusCode: StatusCodes.Status404NotFound),
            JoinResponse.RequirementsFailure(var errors) => TypedResults.ValidationProblem(
                errors: errors,
                detail: "Failed to meet one or more join requirements."),
            _ => throw new ArgumentOutOfRangeException(nameof(memberService.JoinGuildAsync),
                "Unexpected response from JoinGuildAsync.")
        };
}

public static class MemberExtensions
{
    public static IQueryable<ServerMember> ApplySortOrder(
        this IQueryable<ServerMember> query, MemberRequests.EMemberSorters sortBy, EOrder order) => sortBy switch
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