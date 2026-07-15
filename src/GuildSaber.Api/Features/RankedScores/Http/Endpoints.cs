using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Shared;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.AspNetCore.Http.HttpResults;
using ServerRankedScore = GuildSaber.Database.Models.Server.RankedScores.RankedScore;
using ConfirmationResponse = GuildSaber.Api.Features.RankedScores.RankedScoreService.ConfirmationResponse;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses;

namespace GuildSaber.Api.Features.RankedScores.Http;

public class RankedScoreEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contexts/{contextId}/ranked-scores")
            .WithTag("Context.RankedScores", description: "Endpoints for managing guild context ranked scores.");

        group.MapGet("/", GetRankedScoresAsync)
            .WithName("GetRankedScores")
            .WithSummary("Get all ranked scores paginated")
            .WithDescription("Get all ranked scores of a specific context by its Id, with optional sorting.");

        var withScoreGroup = group.MapGroup("/scores")
            .WithTag("Context.RankedScores",
                description: "Endpoints for managing ranked scores from their underlying scoreIds.");

        withScoreGroup.MapPost("/{scoreId}/set-confirmed", SetConfirmedRankedScoresFromScoreIdAsync)
            .WithName("SetRankedScoresFromScoreIdToConfirmed")
            .WithSummary("Confirm all pending-compatible ranked scores from their underlying scoreId")
            .WithDescription(
                "Set all pending-compatible ranked scores from their underlying scoreId as confirmed by the scoring team.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireGuildPermission(EPermission.ScoringTeam);

        withScoreGroup.MapPost("/{scoreId}/set-refused", SetRefusedRankedScoresFromScoreIdAsync)
            .WithName("SetRankedScoreFromScoreIdToRefused")
            .WithSummary("Refuse all pending-compatible ranked scores from their underlying scoreId")
            .WithDescription(
                "Set all pending-compatible ranked scores from their underlying scoreId as refused by the scoring team.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireGuildPermission(EPermission.ScoringTeam);

        withScoreGroup.MapPost("/{scoreId}/revert-to-pending", RevertRankedScoresFromScoreIdToPendingAsync)
            .WithName("RevertRankedScoresFromScoreIdToPending")
            .WithSummary("Revert all pending-compatible ranked scores from their underlying scoreId back to pending")
            .WithDescription("Set all pending-compatible ranked scores from their underlying scoreId back to pending.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireGuildPermission(EPermission.ScoringTeam);

        var fromPlayerGroup = endpoints.MapGroup("/players")
            .WithTag("Players.RankedScores", description: "Endpoints for managing players' ranked scores.");

        fromPlayerGroup.MapGet("/{playerId}/contexts/{contextId}/ranked-scores", GetPlayerRankedScoresAsync)
            .WithName("GetPlayerRankedScores")
            .WithSummary("Get all ranked scores of a player paginated")
            .WithDescription("Get all ranked scores of a specific player by their Id, with optional sorting.");

        fromPlayerGroup.MapGet("/@me/contexts/{contextId}/ranked-scores", GetPlayerRankedScoresAtMeAsync)
            .WithName("GetPlayerRankedScoresAtMe")
            .WithSummary("Get current player's ranked scores paginated")
            .WithDescription(
                "Get the current player's ranked scores using their player id from claims, with optional sorting.")
            .RequireAuthorization();

        fromPlayerGroup.MapGet("/{playerId}/contexts/{contextId}/ranked-scores-with-ranked-map",
                GetPlayerRankedScoresWithRankedMapAsync)
            .WithName("GetPlayerRankedScoresWithRankedMap")
            .WithSummary("Get all ranked scores of a player with ranked map paginated")
            .WithDescription(
                "Get all ranked scores of a specific player by their Id along with ranked map, with optional sorting.");

        fromPlayerGroup.MapGet("/@me/contexts/{contextId}/ranked-scores-with-ranked-map",
                GetPlayerRankedScoresWithRankedMapAtMeAsync)
            .WithName("GetPlayerRankedScoresWithRankedMapAtMe")
            .WithSummary("Get current player's ranked scores with ranked map paginated")
            .WithDescription(
                "Get the current player's ranked scores along with ranked map using their player id from claims, with optional sorting.")
            .RequireAuthorization();
    }

    public static async Task<Ok<PagedList<RankedScoreWithRankedMap>>> GetRankedScoresAsync(
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await dbContext.RankedScores.AsExpandable()
            .Where(x => x.ContextId == contextId && x.IsSelected)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreWithRankedMapExpression)
            .ToPagedListAsync(page, pageSize));

    public static async Task<Ok<PagedList<RankedScoreResponses.RankedScore>>> GetPlayerRankedScoresAtMeAsync(
        ClaimsPrincipal claimsPrincipal,
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc)
        => await GetPlayerRankedScoresAsync(claimsPrincipal.GetPlayerId()!.Value, contextId, dbContext, filters, page,
            pageSize, sortBy, order);

    public static async Task<Ok<PagedList<RankedScoreResponses.RankedScore>>> GetPlayerRankedScoresAsync(
        [FromRoute] PlayerId playerId,
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await dbContext.RankedScores
            .Where(x => x.PlayerId == playerId && x.ContextId == contextId && x.IsSelected)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreExpression())
            .ToPagedListAsync(page, pageSize));

    public static async Task<Ok<PagedList<RankedScoreWithRankedMap>>> GetPlayerRankedScoresWithRankedMapAtMeAsync(
        ClaimsPrincipal claimsPrincipal,
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc)
        => await GetPlayerRankedScoresWithRankedMapAsync(claimsPrincipal.GetPlayerId()!.Value, contextId, dbContext,
            filters, page, pageSize, sortBy, order);

    public static async Task<Ok<PagedList<RankedScoreWithRankedMap>>> GetPlayerRankedScoresWithRankedMapAsync(
        [FromRoute] PlayerId playerId,
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc)
        => TypedResults.Ok(await dbContext.RankedScores.AsExpandable()
            .Where(x => x.PlayerId == playerId && x.ContextId == contextId && x.IsSelected)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreWithRankedMapExpression)
            .ToPagedListAsync(page, pageSize));

    private static Task<Results<Ok<RankedScoreResponses.RankedScore[]>, NotFound, ProblemHttpResult>>
        SetConfirmedRankedScoresFromScoreIdAsync(
            [FromRoute] ContextId contextId,
            [FromRoute] ScoreId scoreId,
            RankedScoreService rankedScoreService,
            CancellationToken token)
        => HandleConfirmationResponseAsync(rankedScoreService.SetConfirmedAsync(contextId, scoreId, token));

    private static Task<Results<Ok<RankedScoreResponses.RankedScore[]>, NotFound, ProblemHttpResult>>
        SetRefusedRankedScoresFromScoreIdAsync(
            [FromRoute] ContextId contextId,
            [FromRoute] ScoreId scoreId,
            RankedScoreService rankedScoreService,
            CancellationToken token)
        => HandleConfirmationResponseAsync(rankedScoreService.SetRefusedAsync(contextId, scoreId, token));

    private static Task<Results<Ok<RankedScoreResponses.RankedScore[]>, NotFound, ProblemHttpResult>>
        RevertRankedScoresFromScoreIdToPendingAsync(
            [FromRoute] ContextId contextId,
            [FromRoute] ScoreId scoreId,
            RankedScoreService rankedScoreService,
            CancellationToken token)
        => HandleConfirmationResponseAsync(rankedScoreService.RevertToPendingAsync(contextId, scoreId, token));

    private static async Task<Results<Ok<RankedScoreResponses.RankedScore[]>, NotFound, ProblemHttpResult>>
        HandleConfirmationResponseAsync(Task<ConfirmationResponse> responseTask)
        => await responseTask switch
        {
            ConfirmationResponse.Success(var rankedScores) => TypedResults.Ok(rankedScores.Select(x => x.Map())
                .ToArray()),
            ConfirmationResponse.NotFound => TypedResults.NotFound(),
            _ => throw new ArgumentOutOfRangeException(nameof(responseTask), "Unexpected confirmation response.")
        };
}

public static class RankedScoreEndpointExtensions
{
    extension(IQueryable<ServerRankedScore> query)
    {
        public IQueryable<ServerRankedScore> ApplyFilters(Filters filters)
        {
            if (filters.RankedScoreTypes is not ERankedScoreType.None)
            {
                var validFilter = filters.RankedScoreTypes.HasFlag(ERankedScoreType.Valid);
                var invalidFilter = filters.RankedScoreTypes.HasFlag(ERankedScoreType.Invalid);
                var pendingFilter = filters.RankedScoreTypes.HasFlag(ERankedScoreType.Pending);
                var acceptedFilter = filters.RankedScoreTypes.HasFlag(ERankedScoreType.Accepted);
                var refusedFilter = filters.RankedScoreTypes.HasFlag(ERankedScoreType.Refused);

                query = query.Where(x =>
                    validFilter && x is ValidRankedScore
                    || invalidFilter && x is InvalidRankedScore
                    || pendingFilter && x is PendingRankedScore
                    || acceptedFilter && x is AcceptedRankedScore
                    || refusedFilter && x is RefusedRankedScore);
            }

            if (filters.DifficultyStarFrom is { } difficultyStarFrom)
                query = query.Where(x => x.RankedMap.Rating.DiffStar >= difficultyStarFrom);

            if (filters.DifficultyStarTo is { } difficultyStarTo)
                query = query.Where(x => x.RankedMap.Rating.DiffStar <= difficultyStarTo);

            if (filters.AccuracyStarFrom is { } accuracyStarFrom)
                query = query.Where(x => x.RankedMap.Rating.AccStar >= accuracyStarFrom);

            if (filters.AccuracyStarTo is { } accuracyStarTo)
                query = query.Where(x => x.RankedMap.Rating.AccStar <= accuracyStarTo);

            if (filters.BpmFrom is { } bpmFrom)
                query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM >= bpmFrom));

            if (filters.BpmTo is { } bpmTo)
                query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM <= bpmTo));

            return query;
        }

        public IQueryable<ServerRankedScore> ApplySortOrder(ERankedScoreSorter sortBy, EOrder order) => sortBy switch
        {
            ERankedScoreSorter.Points => query
                .OrderBy(x => x is PointGivingRankedScore ? 1 : 0)
                .ThenBy(order, x => x is ScoredRankedScore ? ((ScoredRankedScore)x).RawPoints : default)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.DifficultyStar => query
                .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
                .ThenBy(order, x => x.RankedMap.Rating.DiffStar)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.AccuracyStar => query
                .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
                .ThenBy(order, x => x.RankedMap.Rating.AccStar)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.Score => query
                .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
                .ThenBy(order, x => x.EffectiveScore)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.Accuracy => query
                .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
                .ThenBy(order, x => x.EffectiveScore / x.SongDifficulty.Stats.MaxScore)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.ScoreTime => query
                .OrderBy(x => x is PointGivingRankedScore ? 0 : 1)
                .ThenBy(order, x => x.Score.SetAt)
                .ThenBy(x => x.Id),
            ERankedScoreSorter.EditTime => query
                .OrderBy(order, x => x.EditedAt)
                .ThenBy(x => x.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };
    }
}