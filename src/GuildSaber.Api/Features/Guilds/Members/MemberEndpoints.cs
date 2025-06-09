using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Database.Models.Server.Guilds.GuildRequirements.Requirements;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace GuildSaber.Api.Features.Guilds.Members;

public class MemberEndpoints : IEndPoints
{
    public const string GetMemberName = "GetMember";

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
            .ProducesValidationProblem()
            .RequireManager();
    }

    private static async Task<Result<(GuildRequirements requirements, PlayerResponseFullWithStats blProfile)>>
        TryGetGuildRequirementsAndProfile(
            ServerDbContext dbContext, BeatLeaderApi beatLeaderApi, Guild.GuildId guildId,
            Player.PlayerId playerId)
    {
        var guild = await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .Select(x => new { x.Requirements })
            .FirstOrDefaultAsync();

        if (guild is null)
            return Failure<(GuildRequirements, PlayerResponseFullWithStats)>(
                $"Guild with ID {guildId} does not exist.");

        var blId = await dbContext.Players.Where(x => x.Id == playerId)
            .Select(x => x.LinkedAccounts.BeatLeaderId)
            .FirstOrDefaultAsync();

        var result = await beatLeaderApi.GetPlayerProfileWithStats(blId);
        return result.TryGetValue(out var profile) is false
            ? Failure<(GuildRequirements, PlayerResponseFullWithStats)>(
                $"Failed to retrieve BeatLeader profile with id:${blId}.")
            : (guild.Requirements, profile);
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
        Guild.GuildId guildId, ServerDbContext dbContext, BeatLeaderApi beatLeaderApi,
        TimeProvider timeProvider, LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var playerId = httpContext.User.GetPlayerId();
        if (playerId is null) return Results.Unauthorized();

        return await JoinGuildAsync(guildId, playerId.Value, dbContext, beatLeaderApi, timeProvider,
            linkGenerator, httpContext);
    }

    /// <summary>
    /// Handles a player's request to join a specific guild
    /// </summary>
    /// <param name="guildId">The unique identifier of the guild to join</param>
    /// <param name="playerId">The unique identifier of the player requesting to join</param>
    /// <param name="dbContext">Database context for data operations</param>
    /// <param name="beatLeaderApi">Service to access BeatLeader player statistics</param>
    /// <param name="timeProvider">Provider for consistent time values</param>
    /// <param name="linkGenerator">URL generator for endpoint responses</param>
    /// <param name="httpContext">Current HTTP context information</param>
    /// <returns>
    ///     <para>200 OK with <see cref="MemberResponses.Member" /> when player joins successfully</para>
    ///     <para>202 Accepted with <see cref="MemberResponses.Member" /> when join request is submitted for approval</para>
    ///     <para>400 Bad Request with validation problem details when player fails to meet guild requirements</para>
    ///     <para>409 Conflict when player is already a member of the guild</para>
    ///     <para>500 Internal Server Error when member entry cannot be saved</para>
    /// </returns>
    /// <remarks>
    /// The method checks if the player meets all guild requirements before allowing them to join.
    /// If guild has submission requirement flag, the player's join request will need approval.
    /// </remarks>
    private static async Task<IResult> JoinGuildAsync(
        Guild.GuildId guildId, Player.PlayerId playerId,
        ServerDbContext dbContext, BeatLeaderApi beatLeaderApi, TimeProvider timeProvider,
        LinkGenerator linkGenerator, HttpContext httpContext)
        => await GetMemberAsFailure(dbContext, guildId, playerId)
            .MapError(Results.Conflict)
            .Bind(() => TryGetGuildRequirementsAndProfile(dbContext, beatLeaderApi, guildId, playerId)
                .MapError(error => Results.Problem(error, statusCode: StatusCodes.Status400BadRequest)))
            .Check(x => GetRequirementsErrorAsFailure(x.requirements, x.blProfile)
                .MapError(errors => Results.ValidationProblem(
                    errors: errors,
                    detail: "Failed to meet one or more join requirements",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Join Requirements Not Met")))
            .Map(static async (x, context) => CreateMember(
                    context.playerId,
                    context.guildId,
                    x.requirements.Flags.HasFlag(Submission) ? Member.EJoinState.Requested : Member.EJoinState.Joined,
                    await GetNextPriority(context.dbContext, context.playerId), context.timeProvider),
                (playerId, guildId, timeProvider, dbContext))
            .Bind(member => dbContext
                .AddAndSaveAsync(member, x => x.Map())
                .MapError(ex => Results.Problem(ex.ToString(), statusCode: 500, title: "Error saving member")))
            .Match(
                member => member.JoinState == MemberResponses.EJoinState.Requested
                    ? Results.Accepted(linkGenerator.GetUriByName(httpContext, GetMemberName,
                        new { guildId, playerId }), member)
                    : Results.Ok(member),
                err => err
            );

    private static UnitResult<IEnumerable<KeyValuePair<string, string[]>>> GetRequirementsErrorAsFailure(
        GuildRequirements requirements, PlayerResponseFullWithStats profile) => requirements.Flags.GetFlags()
            .Select<GuildRequirements.Requirements, KeyValuePair<string, string[]>?>(reqEnum
                => reqEnum switch
                {
                    MinRank when profile.Rank < requirements.MinRank => new KeyValuePair<string, string[]>(
                        nameof(MinRank),
                        [$"Your rank ({profile.Rank}) is below the minimum required rank ({requirements.MinRank})."]),
                    MaxRank when profile.Rank > requirements.MaxRank => new KeyValuePair<string, string[]>(
                        nameof(MaxRank),
                        [$"Your rank ({profile.Rank}) is above the maximum allowed rank ({requirements.MaxRank})."]),
                    MinPP when profile.Pp < requirements.MinPP => new KeyValuePair<string, string[]>(nameof(MinPP),
                        [$"Your PP ({profile.Pp}) is below the minimum required PP ({requirements.MinPP})."]),
                    MaxPP when profile.Pp > requirements.MaxPP => new KeyValuePair<string, string[]>(nameof(MaxPP),
                        [$"Your PP ({profile.Pp}) is above the maximum allowed PP ({requirements.MaxPP})."]),
                    AccountAgeUnix when profile.ScoreStats.FirstScoreTime < requirements.AccountAgeUnix
                        => new KeyValuePair<string, string[]>(nameof(AccountAgeUnix),
                        [
                            $"Your account age ({profile.ScoreStats.FirstScoreTime}) is below the minimum required account age ({requirements.AccountAgeUnix})."
                        ]),
                    _ => null
                })
            .Where(x => x is not null)
            .Select(errors => errors!.Value).ToArray() switch
        {
            var errors when errors.Length != 0 => Failure<IEnumerable<KeyValuePair<string, string[]>>>(errors),
            _ => UnitResult.Success<IEnumerable<KeyValuePair<string, string[]>>>()
        };

    private static async Task<UnitResult<MemberResponses.Member>> GetMemberAsFailure(
        ServerDbContext dbContext, Guild.GuildId guildId, Player.PlayerId playerId)
        => await dbContext.Members.FirstOrDefaultAsync(x => x.GuildId == guildId && x.PlayerId == playerId) switch
        {
            null => UnitResult.Success<MemberResponses.Member>(),
            var member => Failure(member.Map())
        };

    private static Member CreateMember(
        Player.PlayerId playerId, Guild.GuildId guildId, Member.EJoinState joinState,
        int priority, TimeProvider timeProvider)
        => new()
        {
            PlayerId = playerId,
            GuildId = guildId,
            CreatedAt = timeProvider.GetUtcNow(),
            EditedAt = timeProvider.GetUtcNow(),
            JoinState = joinState,
            Permissions = Member.EPermission.None,
            Priority = priority
        };

    private static async Task<int> GetNextPriority(ServerDbContext dbContext, Player.PlayerId playerId)
        => (await dbContext.Members
                .Where(x => x.PlayerId == playerId)
                .Select(x => x.Priority)
                .ToListAsync())
            .Aggregate(1, (current, session) => session > current ? current : current + 1);
}