using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Shared;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static GuildSaber.Api.Features.MapRequests.Http.MapProposalRequests;
using static GuildSaber.Api.Features.MapRequests.Http.MapRequestResponses;

namespace GuildSaber.Api.Features.MapRequests.Http;

public sealed class MapRequestEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("contexts/{contextId}/map-requests")
            .WithTag("Context.MapRequests",
                description: "Thread-based ranking requests, immutable proposals, votes and boosts.");

        group.MapGet("/", GetQueueAsync)
            .WithOptionalAuthentication()
            .WithName("GetMapRequestQueue")
            .WithSummary("Get the ranking queue as threads containing submitted proposals only.")
            .WithDescription(
                "Get threads containing submitted proposals only. Ordering is calculated for each request, so "
                + "positions may change between pages. Use sortBy and order to select the sort field and direction. "
                + "Priority defaults to descending. WaitingSince ascending shows the oldest requests first. "
                + "BoostCount and CommunityVotes ties use oldest waiting time. Final ties use thread ID ascending. "
                + "Ranking-team votes do not affect ordering. Use the thread endpoint to read proposal history.");

        group.MapGet("/{threadId}", GetThreadAsync)
            .WithOptionalAuthentication()
            .WithName("GetMapRequestThread")
            .WithSummary("Get a thread's visible proposal history.")
            .WithDescription(
                "Get published proposals in creation order, including the current player's private drafts. "
                + "Other players' drafts are hidden, including from reviewers.");

        group.MapGet("/proposals/@me", GetMineAsync)
            .RequireAuthorization()
            .WithName("GetMyMapProposals")
            .WithSummary("Get your proposals in this context, including drafts and history.")
            .WithDescription("Use sortBy and order to select the sort field and direction. Defaults to CreatedAt "
                             + "descending, with proposal ID in the same direction to break ties.");

        group.MapGet("/allowance/@me", GetAllowanceAsync)
            .RequireAuthorization()
            .WithName("GetMyMapRequestAllowance")
            .WithSummary("Get your per-context proposal usage and the configured submission, review and queue rules.")
            .WithDescription(
                "Limits count proposals per player and context, not threads. New-ranking and revision "
                + "proposals have separate limits. Boost allowances are global and returned by GET "
                + "/players/@me/map-request-boosts.");

        group.MapPost("/", CreateThreadAsync)
            .RequireAuthorization()
            .WithName("CreateMapRequest")
            .WithSummary("Create a ranking-request thread and its first private draft.")
            .WithDescription(
                "Create a thread in this context without publishing it or ranking a map. Song versions and "
                + "leaderboard IDs are resolved now and missing ratings are calculated on submission. The draft "
                + "will need to be submitted and explicitly accepted before a ranked map can be created. Exceeding the "
                + "maximum allowed draft count returns 429 with code 'draft_limit' and limit. BeatSaver rate limits return "
                + "'upstream_rate_limit' and retryAfterSeconds.")
            .Produces<Proposal>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        group.MapPost("/{threadId}/proposals", CreateProposalAsync)
            .AddEndpointFilter(ValidateAmendmentPermissionAsync)
            .RequireAuthorization()
            .WithName("CreateMapProposal")
            .WithSummary("Create a private draft with a complete alternative configuration in an existing thread.")
            .WithDescription(
                "Supply a complete configuration, not a patch, whether the thread already has a ranked map or "
                + "not. The draft remains private until submitted. Competing with another author's submitted "
                + "proposal may require a configured subscription tier. Ranking-team members, guild leaders and "
                + "managers bypass that restriction.")
            .Produces<Proposal>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        group.MapGet("/{threadId}/proposals/{proposalId}", GetProposalAsync)
            .WithOptionalAuthentication()
            .WithName("GetMapProposal")
            .WithSummary("Get a published proposal or your own private draft.");

        var proposals = group.MapGroup("/{threadId}/proposals/{proposalId}")
            .AddEndpointFilter(ValidateProposalRouteAsync);

        proposals.MapPost("/submit", SubmitAsync)
            .RequireAuthorization()
            .WithName("SubmitMapProposal")
            .WithSummary("Publish your private draft as an immutable proposal in the ranking queue.")
            .WithDescription(
                "Publish the author's draft after resolving missing ratings. For threads without a ranked map, "
                + "any shared difficulty in another ranked map or submitted thread in this context returns 409 "
                + "with code 'duplicate_maps' and duplicates containing threadId and either rankedMapId or "
                + "proposalId. Review those matches before retrying with force=true. Revisions skip this "
                + "duplicate check. Failed rating calculation returns 502 with code 'rating_calculation' and "
                + "leaves the proposal as a draft.")
            .Produces<Proposal>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        proposals.MapPost("/close", CloseAsync)
            .RequireAuthorization()
            .WithName("CloseMapProposal")
            .WithSummary("Withdraw your submitted proposal, retaining its configuration and votes in history.")
            .WithDescription(
                "Only the author can withdraw a submitted proposal. Its configuration and votes remain in "
                + "history. It cannot be reopened. Boosts are released when no submitted proposals remain. Use "
                + "DELETE to discard a private draft.")
            .Produces<Proposal>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapDelete("/", DeleteDraftAsync)
            .RequireAuthorization()
            .WithName("DeleteMapProposalDraft")
            .WithSummary("Permanently delete your private draft.")
            .WithDescription(
                "Only the author's private drafts can be deleted. Missing drafts, other authors' drafts and "
                + "published proposals return 404.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        proposals.MapPost("/accept", AcceptAsync)
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader)
            .WithName("AcceptMapProposal")
            .WithSummary(
                "Explicitly accept a submitted proposal and apply its complete configuration to the ranked "
                + "map.")
            .WithDescription(
                "Requires ranking-team, guild-leader or manager permissions. Ranking-team vote requirements "
                + "apply unless a guild leader or manager accepts. Community votes do not count. Acceptance "
                + "creates or updates the ranked map, closes competing submitted proposals and releases boosts. "
                + "Private drafts remain open. Score/stat refresh is queued afterwards. Unmet vote requirements "
                + "return 409 with code 'vote_requirements', positiveVotes, negativeVotes, requiredPositiveVotes "
                + "and requireNoNegativeVotes.")
            .Produces<Acceptance>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapPost("/deny", DenyAsync)
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader)
            .WithName("DenyMapProposal")
            .WithSummary("Deny a submitted proposal as a reviewer, retaining its configuration and votes in history.")
            .WithDescription(
                "Requires ranking-team, guild-leader or manager permissions. Retains the proposal and its "
                + "votes without changing the ranked map. Boosts are released when no submitted proposals "
                + "remain.")
            .Produces<Proposal>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapPut("/votes/community/@me", VoteCommunityAsync)
            .RequireAuthorization()
            .WithName("VoteCommunityOnMapProposal")
            .WithSummary("Cast or replace your community vote on a submitted proposal.")
            .WithDescription(
                "A player can have one community vote and a separate ranking-team vote per proposal. Community "
                + "votes affect CommunityVotes ordering, not acceptance requirements. Votes can only change "
                + "while the proposal is submitted.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapDelete("/votes/community/@me", RemoveCommunityVoteAsync)
            .RequireAuthorization()
            .WithName("RemoveMapProposalCommunityVote")
            .WithSummary("Remove your community vote while the proposal is submitted.")
            .WithDescription(
                "Only submitted proposals allow vote removal. Returns 204 if no community vote exists. "
                + "Historical votes cannot be removed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapPut("/votes/ranking-team/@me", VoteRankingTeamAsync)
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader)
            .WithName("VoteRankingTeamOnMapProposal")
            .WithSummary("Cast or replace your ranking-team vote and optional reason on a submitted proposal.")
            .WithDescription(
                "Requires ranking-team, guild-leader or manager permissions. Counts towards acceptance "
                + "requirements, but acceptance is always explicit. Replaces the current player's ranking-team "
                + "vote and reason without changing their community vote. Only submitted proposals can be voted "
                + "on.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        proposals.MapDelete("/votes/ranking-team/@me", RemoveRankingTeamVoteAsync)
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader)
            .WithName("RemoveMapProposalRankingTeamVote")
            .WithSummary("Remove your ranking-team vote while the proposal is submitted.")
            .WithDescription(
                "Requires ranking-team, guild-leader or manager permissions. Only submitted proposals allow "
                + "vote removal. Returns 204 if no ranking-team vote exists. Does not remove the player's "
                + "community vote.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{threadId}/boosts/@me", BoostAsync)
            .RequireAuthorization()
            .WithName("BoostMapRequest")
            .WithSummary("Allocate one of your global boosts to a thread with submitted proposals.")
            .WithDescription(
                "The thread must have a submitted proposal. At most one boost per player per thread. Repeating "
                + "this request does not consume another slot. Boosts affect ordering, not acceptance "
                + "requirements, and are released on acceptance or when no submitted proposals remain. The "
                + "allowance is shared across all guilds and contexts. Exhaustion returns 429 with code "
                + "'boost_limit' and limit.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/{threadId}/boosts/@me/move", MoveBoostAsync)
            .RequireAuthorization()
            .WithName("MoveMapRequestBoost")
            .WithSummary(
                "Transfer your existing boost to another queued thread, without consuming another allowance "
                + "slot.")
            .WithDescription(
                "Transfer the player's boost from sourceThreadId to the thread in this route, including across "
                + "contexts or guilds. The destination must have a submitted proposal. Moving to a different "
                + "thread already boosted by that player returns 409. No additional slot is consumed, but excess "
                + "allocations after a tier downgrade must be released first. No boost history is retained.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapDelete("/{threadId}/boosts/@me", RemoveBoostAsync)
            .RequireAuthorization()
            .WithName("RemoveMapRequestBoost")
            .WithSummary("Release your boost on this thread back to your global allowance.")
            .WithDescription(
                "Release the allocation without changing proposal votes. Returns 204 even if no allocation "
                + "exists. No boost history is retained.")
            .Produces(StatusCodes.Status204NoContent);

        endpoints.MapGet("players/@me/map-request-boosts", GetBoostsAsync)
            .RequireAuthorization()
            .WithTags("MapRequests")
            .WithName("GetMyMapRequestBoosts")
            .WithSummary("Get your boost limit and current allocations across all guilds and contexts.")
            .WithDescription(
                "The allowance is shared across all guilds and contexts and depends on the player's current "
                + "subscription tier and server configuration.");
    }

    private static async ValueTask<object?> ValidateProposalRouteAsync(
        EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var http = invocation.HttpContext;
        if (!ContextId.TryParse(http.Request.RouteValues["contextId"]?.ToString() ?? "", out var contextId)
            || !MapRequestThreadId.TryParse(http.Request.RouteValues["threadId"]?.ToString() ?? "", out var threadId)
            || !MapProposalId.TryParse(http.Request.RouteValues["proposalId"]?.ToString() ?? "", out var proposalId))
            return TypedResults.BadRequest();
        var db = http.RequestServices.GetRequiredService<ServerDbContext>();
        if (!await db.MapProposals.Where(MapRequestQueries.VisibleTo(http.User.GetPlayerId()))
                .AnyAsync(x => x.Id == proposalId && x.ThreadId == threadId && x.Thread.ContextId == contextId,
                    http.RequestAborted))
            return TypedResults.NotFound();
        return await next(invocation);
    }

    private static async ValueTask<object?> ValidateAmendmentPermissionAsync(
        EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var http = invocation.HttpContext;
        var minimumTier = http.RequestServices.GetRequiredService<IOptions<MapRequestSettings>>()
            .Value.MinimumAmendmentTier;
        if (minimumTier == 0)
            return await next(invocation);

        if (!ContextId.TryParse(http.Request.RouteValues["contextId"]?.ToString() ?? "", out var contextId)
            || !MapRequestThreadId.TryParse(http.Request.RouteValues["threadId"]?.ToString() ?? "", out var threadId))
            return TypedResults.BadRequest();

        var playerId = http.User.GetPlayerId()!.Value;
        var dbContext = http.RequestServices.GetRequiredService<ServerDbContext>();
        if (!await dbContext.MapProposals.OfType<SubmittedProposal>()
                .AnyAsync(x => x.ThreadId == threadId && x.Thread.ContextId == contextId && x.AuthorId != playerId,
                    http.RequestAborted)
            || await CanAmendAsync(http))
            return await next(invocation);

        return Problem(StatusCodes.Status403Forbidden, "amendment_tier",
            "Your subscription tier cannot propose a competing variant.");
    }

    private static async Task<bool> CanAmendAsync(HttpContext http)
    {
        var minimumTier = http.RequestServices.GetRequiredService<IOptions<MapRequestSettings>>()
            .Value.MinimumAmendmentTier;
        return minimumTier == 0
               || await http.RequestServices.GetRequiredService<ServerDbContext>().Players
                   .AnyAsync(x => x.Id == http.User.GetPlayerId()
                                  && (int)x.SubscriptionInfo.Tier >= minimumTier, http.RequestAborted)
               || (await http.RequestServices.GetRequiredService<IAuthorizationService>().AuthorizeAsync(
                   http.User, null, new GuildPermissionRequirement(
                       EPermission.RankingTeam | EPermission.GuildLeader, MatchAny: true))).Succeeded;
    }

    /// <summary>
    /// Gets a paginated ranking queue of threads containing submitted proposals.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="queries">Ranking request queries.</param>
    /// <param name="user">The current viewer, when authenticated.</param>
    /// <param name="filters">Filters to apply to the ranking queue.</param>
    /// <param name="token">Cancellation token.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of threads per page.</param>
    /// <param name="sortBy">
    /// Priority: waiting seconds * (1 + boost count * configured bonus).
    /// WaitingSince: earliest currently submitted proposal's submission time.
    /// BoostCount: active thread boosts. CommunityVotes: positive minus negative community votes on submitted proposals.
    /// BoostCount and CommunityVotes ties show oldest waiting requests first. All sorts break final ties by thread ID
    /// ascending. Ranking-team votes do not affect queue ordering.
    /// </param>
    /// <param name="order">
    /// Sort direction. Descending Priority shows highest priority first. Ascending WaitingSince shows
    /// oldest first.
    /// </param>
    public static async Task<Ok<PagedList<MapRequestResponses.Thread>>> GetQueueAsync(
        ContextId contextId,
        MapRequestQueries queries,
        ClaimsPrincipal user,
        [AsParameters] Filters filters,
        CancellationToken token,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        EMapRequestSorter sortBy = EMapRequestSorter.Priority,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await queries.GetQueueAsync(
            contextId, user.GetPlayerId(), filters, sortBy, order, page, pageSize, token));

    private static async Task<Results<Ok<MapRequestResponses.Thread>, NotFound>> GetThreadAsync(
        ContextId contextId, MapRequestThreadId threadId, MapRequestQueries queries, ClaimsPrincipal user,
        CancellationToken token)
        => await queries.GetThreadAsync(contextId, threadId, user.GetPlayerId(), token) is { } thread
            ? TypedResults.Ok(thread)
            : TypedResults.NotFound();

    /// <summary>
    /// Gets your proposals in a context, including drafts and published history.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="queries">Ranking request queries.</param>
    /// <param name="user">The authenticated player.</param>
    /// <param name="filters">Filters to apply to your proposals.</param>
    /// <param name="token">Cancellation token.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of proposals per page.</param>
    /// <param name="sortBy">Sort by proposal creation time or ID. Creation-time ties use ID in the same direction.</param>
    /// <param name="order">Sort direction. Descending CreatedAt shows newest proposals first.</param>
    public static async Task<Ok<PagedList<Proposal>>> GetMineAsync(
        ContextId contextId,
        MapRequestQueries queries,
        ClaimsPrincipal user,
        [AsParameters] Filters filters,
        CancellationToken token,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        EMapProposalSorter sortBy = EMapProposalSorter.CreatedAt,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await queries.GetMineAsync(
            contextId, user.GetPlayerId()!.Value, filters, sortBy, order, page, pageSize, token));

    private static async Task<Ok<Allowance>> GetAllowanceAsync(
        ContextId contextId, MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => TypedResults.Ok(await queries.GetAllowanceAsync(contextId, user.GetPlayerId()!.Value, token));

    private static async Task<Results<Ok<Proposal>, NotFound>> GetProposalAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, MapRequestQueries queries,
        ClaimsPrincipal user, CancellationToken token)
        => await queries.GetProposalAsync(contextId, threadId, proposalId, user.GetPlayerId(), token) is { } proposal
            ? TypedResults.Ok(proposal)
            : TypedResults.NotFound();

    private static Task<IResult> CreateThreadAsync(
        ContextId contextId, CreateDraft request, MapProposalDraftService service, MapRequestQueries queries,
        ClaimsPrincipal user, CancellationToken token)
        => CreateAsync(contextId, null, request, service, queries, user, token);

    private static Task<IResult> CreateProposalAsync(
        ContextId contextId, MapRequestThreadId threadId, CreateDraft request, MapProposalDraftService service,
        MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => CreateAsync(contextId, threadId, request, service, queries, user, token);

    private static async Task<IResult> CreateAsync(
        ContextId contextId, MapRequestThreadId? threadId, CreateDraft request, MapProposalDraftService service,
        MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
    {
        var playerId = user.GetPlayerId()!.Value;
        return await service.CreateAsync(contextId, threadId, playerId, request, token) switch
        {
            MapProposalDraftService.CreateResponse.Success(var proposal) => TypedResults.Created(
                $"/contexts/{contextId}/map-requests/{proposal.ThreadId}/proposals/{proposal.Id}",
                await queries.GetProposalAsync(contextId, proposal.ThreadId, proposal.Id, playerId, token)),
            MapProposalDraftService.CreateResponse.ContextNotFound
                or MapProposalDraftService.CreateResponse.ThreadNotFound
                or MapProposalDraftService.CreateResponse.PlayerNotFound => TypedResults.NotFound(),
            MapProposalDraftService.CreateResponse.LimitReached(var limit) =>
                Problem(StatusCodes.Status429TooManyRequests, "draft_limit",
                    "The draft allowance is exhausted.", new Dictionary<string, object?> { ["limit"] = limit }),
            MapProposalDraftService.CreateResponse.ValidationFailure(var errors) =>
                TypedResults.ValidationProblem(errors),
            MapProposalDraftService.CreateResponse.NotOnBeatSaver =>
                Problem(StatusCodes.Status404NotFound, "song_not_found",
                    "The requested BeatSaver version does not exist."),
            MapProposalDraftService.CreateResponse.BeatSaverFailure(var message) =>
                Problem(StatusCodes.Status502BadGateway, "beatsaver_failure",
                    message),
            MapProposalDraftService.CreateResponse.ResolutionFailure(var index, var message) =>
                Problem(StatusCodes.Status502BadGateway,
                    "difficulty_resolution", $"MapVersions[{index}]: {message}"),
            MapProposalDraftService.CreateResponse.RateLimited(var retryAfter) =>
                Problem(StatusCodes.Status429TooManyRequests, "upstream_rate_limit",
                    "BeatSaver is rate limiting requests.",
                    new Dictionary<string, object?> { ["retryAfterSeconds"] = retryAfter.TotalSeconds }),
            MapProposalDraftService.CreateResponse.Conflict => Conflict()
        };
    }

    private static async Task<IResult> SubmitAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId,
        [AsParameters] SubmissionOptions options, MapProposalService service, MapRequestQueries queries,
        HttpContext http, CancellationToken token)
        => await TransitionAsync(
            await service.SubmitAsync(contextId, proposalId, http.User.GetPlayerId()!.Value, token, options.Force,
                bypassAmendmentRequirement: await CanAmendAsync(http)),
            contextId, threadId, proposalId, queries, http.User, token);

    private static async Task<IResult> CloseAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, Close request,
        MapProposalService service, MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => await TransitionAsync(
            await service.CloseAsync(contextId, proposalId, user.GetPlayerId()!.Value, request.Reason, token),
            contextId, threadId, proposalId, queries, user, token);

    private static async Task<IResult> DeleteDraftAsync(
        ContextId contextId, MapProposalId proposalId, MapProposalService service, ClaimsPrincipal user,
        CancellationToken token)
        => await service.DeleteDraftAsync(contextId, proposalId, user.GetPlayerId()!.Value, token) switch
        {
            MapProposalService.TransitionResponse.Deleted => TypedResults.NoContent(),
            _ => TypedResults.NotFound()
        };

    private static async Task<IResult> TransitionAsync(
        MapProposalService.TransitionResponse response, ContextId contextId, MapRequestThreadId threadId,
        MapProposalId proposalId, MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => response switch
        {
            MapProposalService.TransitionResponse.Success => TypedResults.Ok(
                await queries.GetProposalAsync(contextId, threadId, proposalId, user.GetPlayerId(), token)),
            MapProposalService.TransitionResponse.Deleted => TypedResults.NoContent(),
            MapProposalService.TransitionResponse.NotFound => TypedResults.NotFound(),
            MapProposalService.TransitionResponse.Forbidden => Problem(StatusCodes.Status403Forbidden, "not_author",
                "Only the proposal's author can perform this action."),
            MapProposalService.TransitionResponse.AmendmentTierRequired =>
                Problem(StatusCodes.Status403Forbidden, "amendment_tier",
                    "Your subscription tier cannot propose a competing variant."),
            MapProposalService.TransitionResponse.InvalidState =>
                Problem(StatusCodes.Status409Conflict, "invalid_state",
                    "The proposal is no longer in the required state."),
            MapProposalService.TransitionResponse.Conflict => Conflict(),
            MapProposalService.TransitionResponse.LimitReached(var limit) =>
                Problem(StatusCodes.Status429TooManyRequests, "submission_limit",
                    "The active request allowance is exhausted.",
                    new Dictionary<string, object?> { ["limit"] = limit }),
            MapProposalService.TransitionResponse.Duplicates(var matches) =>
                Problem(StatusCodes.Status409Conflict, "duplicate_maps",
                    "These difficulties already appear in another thread. Review the matches before retrying with force=true.",
                    new Dictionary<string, object?> { ["duplicates"] = matches }),
            MapProposalService.TransitionResponse.RatingCalculationFailure(var message) =>
                Problem(StatusCodes.Status502BadGateway,
                    "rating_calculation", message),
            MapProposalService.TransitionResponse.ValidationFailure(var errors) =>
                TypedResults.ValidationProblem(errors)
        };

    private static async Task<IResult> AcceptAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, MapProposalReviewService service,
        MapRequestQueries queries, IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken token)
        => await ReviewAsync(
            await service.AcceptAsync(contextId, proposalId, user.GetPlayerId()!.Value, token,
                bypassVoteRequirements: (await authorization.AuthorizeAsync(user, null,
                    new GuildPermissionRequirement(EPermission.GuildLeader))).Succeeded),
            contextId, threadId, proposalId, queries, user, token);

    private static async Task<IResult> DenyAsync(
        ContextId contextId, MapRequestThreadId threadId, MapProposalId proposalId, Close request,
        MapProposalReviewService service, MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => await ReviewAsync(
            await service.DenyAsync(contextId, proposalId, user.GetPlayerId()!.Value, request.Reason, token),
            contextId, threadId, proposalId, queries, user, token);

    private static async Task<IResult> ReviewAsync(
        MapProposalReviewService.ReviewResponse response, ContextId contextId, MapRequestThreadId threadId,
        MapProposalId proposalId, MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => response switch
        {
            MapProposalReviewService.ReviewResponse.Accepted(var proposal, var mapId, _) => TypedResults.Ok(
                new Acceptance(
                    (Proposal.Accepted)(await queries.GetProposalAsync(contextId, threadId, proposal.Id,
                        user.GetPlayerId(),
                        token))!, mapId)),
            MapProposalReviewService.ReviewResponse.Denied => TypedResults.Ok(
                await queries.GetProposalAsync(contextId, threadId, proposalId, user.GetPlayerId(), token)),
            MapProposalReviewService.ReviewResponse.NotFound => TypedResults.NotFound(),
            MapProposalReviewService.ReviewResponse.InvalidState =>
                Problem(StatusCodes.Status409Conflict, "invalid_state",
                    "Only submitted proposals can be reviewed."),
            MapProposalReviewService.ReviewResponse.Conflict => Conflict(),
            MapProposalReviewService.ReviewResponse.VoteRequirementsNotMet(
                var positive, var negative, var required, var noNegative) => Problem(
                    StatusCodes.Status409Conflict, "vote_requirements",
                    "The ranking-team vote requirements have not been met.",
                    new Dictionary<string, object?>
                    {
                        ["positiveVotes"] = positive,
                        ["negativeVotes"] = negative,
                        ["requiredPositiveVotes"] = required,
                        ["requireNoNegativeVotes"] = noNegative
                    }),
            MapProposalReviewService.ReviewResponse.TooManyRankedMaps(var current, var max) =>
                Problem(StatusCodes.Status409Conflict,
                    "ranked_map_limit", "The guild's ranked-map allowance is exhausted.",
                    new Dictionary<string, object?> { ["current"] = current, ["limit"] = max }),
            MapProposalReviewService.ReviewResponse.ValidationFailure(var errors) =>
                TypedResults.ValidationProblem(errors)
        };

    private static async Task<IResult> VoteCommunityAsync(
        ContextId contextId, MapProposalId proposalId, CommunityVote request, MapProposalVoteService service,
        ClaimsPrincipal user, CancellationToken token)
        => VoteResult(await service.SetCommunityAsync(
            contextId, proposalId, user.GetPlayerId()!.Value, request.IsPositive, token));

    private static async Task<IResult> VoteRankingTeamAsync(
        ContextId contextId, MapProposalId proposalId, RankingTeamVote request, MapProposalVoteService service,
        ClaimsPrincipal user, CancellationToken token)
        => VoteResult(await service.SetRankingTeamAsync(
            contextId, proposalId, user.GetPlayerId()!.Value, request.IsPositive, request.Reason, token));

    private static async Task<IResult> RemoveCommunityVoteAsync(
        ContextId contextId, MapProposalId proposalId, MapProposalVoteService service, ClaimsPrincipal user,
        CancellationToken token)
        => VoteResult(await service.RemoveCommunityAsync(contextId, proposalId, user.GetPlayerId()!.Value, token));

    private static async Task<IResult> RemoveRankingTeamVoteAsync(
        ContextId contextId, MapProposalId proposalId, MapProposalVoteService service, ClaimsPrincipal user,
        CancellationToken token)
        => VoteResult(await service.RemoveRankingTeamAsync(contextId, proposalId, user.GetPlayerId()!.Value, token));

    private static IResult VoteResult(MapProposalVoteService.VoteResponse response)
        => response switch
        {
            MapProposalVoteService.VoteResponse.Success => TypedResults.NoContent(),
            MapProposalVoteService.VoteResponse.NotFound => TypedResults.NotFound(),
            MapProposalVoteService.VoteResponse.InvalidState => Problem(StatusCodes.Status409Conflict, "invalid_state",
                "Votes are only mutable on submitted proposals."),
            MapProposalVoteService.VoteResponse.Conflict => Conflict()
        };

    private static async Task<IResult> BoostAsync(
        ContextId contextId, MapRequestThreadId threadId, MapRequestBoostService service, ClaimsPrincipal user,
        CancellationToken token)
        => BoostResult(await service.AddAsync(contextId, threadId, user.GetPlayerId()!.Value, token));

    private static async Task<IResult> MoveBoostAsync(
        ContextId contextId, MapRequestThreadId threadId, MoveBoost request, MapRequestBoostService service,
        ClaimsPrincipal user, CancellationToken token)
        => BoostResult(await service.MoveAsync(contextId, request.SourceThreadId, threadId, user.GetPlayerId()!.Value,
            token));

    private static async Task<IResult> RemoveBoostAsync(
        ContextId contextId, MapRequestThreadId threadId, MapRequestBoostService service, ClaimsPrincipal user,
        CancellationToken token)
        => BoostResult(await service.RemoveAsync(contextId, threadId, user.GetPlayerId()!.Value, token));

    private static IResult BoostResult(MapRequestBoostService.BoostResponse response)
        => response switch
        {
            MapRequestBoostService.BoostResponse.Success => TypedResults.NoContent(),
            MapRequestBoostService.BoostResponse.ThreadNotFound
                or MapRequestBoostService.BoostResponse.PlayerNotFound
                or MapRequestBoostService.BoostResponse.SourceNotFound => TypedResults.NotFound(),
            MapRequestBoostService.BoostResponse.NoSubmittedProposals =>
                Problem(StatusCodes.Status409Conflict, "no_submitted_proposals",
                    "Only threads with submitted proposals can be boosted."),
            MapRequestBoostService.BoostResponse.AlreadyBoosted =>
                Problem(StatusCodes.Status409Conflict, "already_boosted",
                    "You already boost the target thread."),
            MapRequestBoostService.BoostResponse.LimitReached(var limit) =>
                Problem(StatusCodes.Status429TooManyRequests, "boost_limit",
                    "The global boost allowance is exhausted.", new Dictionary<string, object?> { ["limit"] = limit }),
            MapRequestBoostService.BoostResponse.Conflict => Conflict()
        };

    private static async Task<Results<Ok<BoostAllowance>, NotFound>> GetBoostsAsync(
        MapRequestQueries queries, ClaimsPrincipal user, CancellationToken token)
        => await queries.GetBoostsAsync(user.GetPlayerId()!.Value, token) is { } allowance
            ? TypedResults.Ok(allowance)
            : TypedResults.NotFound();

    private static ProblemHttpResult Conflict() => Problem(StatusCodes.Status409Conflict, "concurrent_change",
        "The request changed concurrently. Refresh before retrying.");

    private static ProblemHttpResult Problem(
        int status, string code, string detail, Dictionary<string, object?>? extensions = null)
    {
        extensions ??= [];
        extensions["code"] = code;
        return TypedResults.Problem(statusCode: status, title: "Map request could not be completed", detail: detail,
            extensions: extensions);
    }
}