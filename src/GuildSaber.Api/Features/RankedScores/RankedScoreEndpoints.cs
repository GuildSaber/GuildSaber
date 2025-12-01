using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using static GuildSaber.Api.Features.RankedScores.RankedScoreResponses;
using ERankedScoreSorter = GuildSaber.Api.Features.RankedScores.RankedScoreRequests.ERankedScoreSorter;
using ServerRankedScore = GuildSaber.Database.Models.Server.RankedScores.RankedScore;

namespace GuildSaber.Api.Features.RankedScores;

public class RankedScoreEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var withPlayerGroup = endpoints.MapGroup("/players")
            .WithTag("Players.RankedScores", description: "Endpoints for managing players' ranked scores.");

        withPlayerGroup.MapGet("/{playerId}/contexts/{contextId}/ranked-scores", GetPlayerRankedScoresAsync)
            .WithName("GetPlayerRankedScores")
            .WithSummary("Get all ranked scores of a player paginated")
            .WithDescription("Get all ranked scores of a specific player by their Id, with optional sorting.");

        withPlayerGroup.MapGet("/@me/contexts/{contextId}/ranked-scores", GetPlayerAtMeRankedScoresAsync)
            .WithName("GetPlayerAtMeRankedScores")
            .WithSummary("Get current player's ranked scores paginated")
            .WithDescription(
                "Get the current player's ranked scores using their player id from claims, with optional sorting.")
            .RequireAuthorization();

        withPlayerGroup.MapGet("/{playerId}/contexts/{contextId}/ranked-scores-with-ranked-map",
                GetPlayerRankedScoresWithRankedMapAsync)
            .WithName("GetPlayerRankedScoresWithRankedMap")
            .WithSummary("Get all ranked scores of a player with ranked map paginated")
            .WithDescription(
                "Get all ranked scores of a specific player by their Id along with ranked map, with optional sorting.");

        withPlayerGroup.MapGet("/@me/contexts/{contextId}/ranked-scores-with-ranked-map",
                GetPlayerAtMeRankedScoresWithRankedMapAsync)
            .WithName("GetPlayerAtMeRankedScoresWithRankedMap")
            .WithSummary("Get current player's ranked scores with ranked map paginated")
            .WithDescription(
                "Get the current player's ranked scores along with ranked map using their player id from claims, with optional sorting.")
            .RequireAuthorization();
    }

    public static Task<Ok<PagedList<RankedScore>>> GetPlayerAtMeRankedScoresAsync(
        ClaimsPrincipal claimsPrincipal,
        ContextId contextId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc,
        float? difficultyStarFrom = null,
        float? accuracyStarFrom = null,
        float? difficultyStarTo = null,
        float? accuracyStarTo = null,
        float? bpmFrom = null,
        float? bpmTo = null
    ) => GetPlayerRankedScoresAsync(claimsPrincipal.GetPlayerId()!.Value, contextId, dbContext, page, pageSize,
        sortBy, order, difficultyStarFrom, accuracyStarFrom, difficultyStarTo, accuracyStarTo, bpmFrom, bpmTo);

    public static async Task<Ok<PagedList<RankedScore>>> GetPlayerRankedScoresAsync(
        PlayerId playerId,
        ContextId contextId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc,
        float? difficultyStarFrom = null,
        float? accuracyStarFrom = null,
        float? difficultyStarTo = null,
        float? accuracyStarTo = null,
        float? bpmFrom = null,
        float? bpmTo = null)
    {
        var query = dbContext.RankedScores
            .Where(x => x.PlayerId == playerId
                        && x.ContextId == contextId
                        && x.State.HasFlag(ServerRankedScore.EState.Selected));

        if (difficultyStarFrom.HasValue)
            query = query.Where(x => x.RankedMap.Rating.DiffStar >= difficultyStarFrom.Value);
        if (difficultyStarTo.HasValue)
            query = query.Where(x => x.RankedMap.Rating.DiffStar <= difficultyStarTo.Value);
        if (accuracyStarFrom.HasValue)
            query = query.Where(x => x.RankedMap.Rating.AccStar >= accuracyStarFrom.Value);
        if (accuracyStarTo.HasValue)
            query = query.Where(x => x.RankedMap.Rating.AccStar <= accuracyStarTo.Value);
        if (bpmFrom.HasValue)
            query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM >= bpmFrom.Value));
        if (bpmTo.HasValue)
            query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM <= bpmTo.Value));

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreExpression(dbContext))
            .ToPagedListAsync(page, pageSize));
    }

    public static Task<Ok<PagedList<RankedScoreWithRankedMap>>> GetPlayerAtMeRankedScoresWithRankedMapAsync(
        ClaimsPrincipal claimsPrincipal,
        ContextId contextId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc,
        float? difficultyStarFrom = null,
        float? accuracyStarFrom = null,
        float? difficultyStarTo = null,
        float? accuracyStarTo = null,
        float? bpmFrom = null,
        float? bpmTo = null
    ) => GetPlayerRankedScoresWithRankedMapAsync(claimsPrincipal.GetPlayerId()!.Value, contextId, dbContext, page,
        pageSize,
        sortBy, order, difficultyStarFrom, accuracyStarFrom, difficultyStarTo, accuracyStarTo, bpmFrom, bpmTo);

    public static async Task<Ok<PagedList<RankedScoreWithRankedMap>>> GetPlayerRankedScoresWithRankedMapAsync(
        PlayerId playerId,
        ContextId contextId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        ERankedScoreSorter sortBy = ERankedScoreSorter.Points,
        EOrder order = EOrder.Desc,
        float? difficultyStarFrom = null,
        float? accuracyStarFrom = null,
        float? difficultyStarTo = null,
        float? accuracyStarTo = null,
        float? bpmFrom = null,
        float? bpmTo = null)
    {
        var query = dbContext.RankedScores
            .Where(x => x.PlayerId == playerId
                        && x.ContextId == contextId
                        && x.State.HasFlag(ServerRankedScore.EState.Selected));

        if (difficultyStarFrom.HasValue)
            query = query.Where(x => x.RankedMap.Rating.DiffStar >= difficultyStarFrom.Value);
        if (difficultyStarTo.HasValue)
            query = query.Where(x => x.RankedMap.Rating.DiffStar <= difficultyStarTo.Value);
        if (accuracyStarFrom.HasValue)
            query = query.Where(x => x.RankedMap.Rating.AccStar >= accuracyStarFrom.Value);
        if (accuracyStarTo.HasValue)
            query = query.Where(x => x.RankedMap.Rating.AccStar <= accuracyStarTo.Value);
        if (bpmFrom.HasValue)
            query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM >= bpmFrom.Value));
        if (bpmTo.HasValue)
            query = query.Where(x => x.RankedMap.MapVersions.Any(y => y.Song.Stats.BPM <= bpmTo.Value));

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreWithRankedMapExpression(dbContext))
            .ToPagedListAsync(page, pageSize));
    }
}