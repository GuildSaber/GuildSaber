using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.Api.Shared;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using RankedMapId = GuildSaber.Database.Models.Server.RankedMaps.RankedMap.RankedMapId;
using PointId = GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId;
using EMemberStatLeaderboardSorter =
    GuildSaber.Api.Features.Leaderboards.Http.LeaderboardRequests.EMemberStatLeaderboardSorter;
using Filters = GuildSaber.Api.Features.Leaderboards.Http.LeaderboardRequests.Filters;

namespace GuildSaber.Api.Features.Leaderboards.Http;

public class LeaderboardEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contexts/{contextId}/points/{pointId}")
            .WithTag("Leaderboards", description: "Endpoints for accessing leaderboards in the server.");

        group.MapGet("/ranked-maps/{rankedMapId}/leaderboard", GetRankedMapLeaderboardAsync)
            .WithName("GetContextPointRankedMapLeaderboard")
            .WithSummary("Get the leaderboard for a ranked map within a context point, paginated.")
            .WithDescription("Get the leaderboard for a ranked map within a context point by its Id, paginated.");

        group.MapGet("/leaderboard", GetMemberPointStatLeaderboardAsync)
            .WithName("GetMemberPointStatLeaderboard")
            .WithSummary("Get the guild member's leaderboard for a point within a context, paginated.")
            .WithDescription("Get the guild member's leaderboard for a point within a context by its Id, paginated.");

        group.MapGet("/categories/{categoryId}/leaderboard", GetMemberCategoryPointStatLeaderboardAsync)
            .WithName("GetMemberCategoryPointStatLeaderboard")
            .WithSummary("Get the guild member's leaderboard for a category point within a context, paginated.")
            .WithDescription("Get the guild member's leaderboard for a category point within a context by its Id," +
                             " paginated.");
    }

    public static async Task<Ok<PagedList<RankedScoreResponses.RankedScoreWithPlayer>>> GetRankedMapLeaderboardAsync(
        ContextId contextId,
        PointId pointId,
        RankedMapId rankedMapId,
        ServerDbContext dbContext,
        [AsParameters] Filters filters,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        LeaderboardRequests.ERankedMapLeaderboardSorter sortBy = LeaderboardRequests.ERankedMapLeaderboardSorter.Points,
        EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.RankedScores
            .Where(x =>
                x.ContextId == contextId &&
                x.PointId == pointId &&
                x.RankedMapId == rankedMapId &&
                x.IsSelected)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreWithPlayerExpression)
            .ToPagedListAsync(page, pageSize)
        );

    public static async Task<Ok<PagedList<LeaderboardResponses.MemberPointStat>>>
        GetMemberPointStatLeaderboardAsync(
            ContextId contextId,
            PointId pointId,
            ServerDbContext dbContext,
            [AsParameters] Filters filters,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 100)] int pageSize = 10,
            EMemberStatLeaderboardSorter sortBy = EMemberStatLeaderboardSorter.Points,
            EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.MemberPointStats
            .Where(x =>
                x.ContextId == contextId &&
                x.PointId == pointId &&
                x.CategoryId == null)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(LeaderboardMappers.MapMemberStatExpression)
            .ToPagedListAsync(page, pageSize)
        );

    public static async Task<Ok<PagedList<LeaderboardResponses.MemberPointStat>>>
        GetMemberCategoryPointStatLeaderboardAsync(
            ContextId contextId,
            PointId pointId,
            CategoryId categoryId,
            ServerDbContext dbContext,
            [AsParameters] Filters filters,
            [Range(1, int.MaxValue)] int page = 1,
            [Range(1, 100)] int pageSize = 10,
            EMemberStatLeaderboardSorter sortBy = EMemberStatLeaderboardSorter.Points,
            EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.MemberPointStats
            .Where(x =>
                x.ContextId == contextId &&
                x.PointId == pointId &&
                x.CategoryId == categoryId)
            .ApplyFilters(filters)
            .ApplySortOrder(sortBy, order)
            .Select(LeaderboardMappers.MapMemberStatExpression)
            .ToPagedListAsync(page, pageSize)
        );
}

public static class LeaderboardExtensions
{
    public static IQueryable<MemberPointStat> ApplyFilters(
        this IQueryable<MemberPointStat> query,
        Filters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(x => EF.Functions.ILike(x.Player.Info.Username, $"%{filters.Search}%"));

        return query;
    }

    public static IQueryable<RankedScore> ApplyFilters(
        this IQueryable<RankedScore> query,
        Filters filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
            query = query.Where(x => EF.Functions.ILike(x.Player.Info.Username, $"%{filters.Search}%"));

        return query;
    }

    public static IQueryable<MemberPointStat> ApplySortOrder(
        this IQueryable<MemberPointStat> query,
        EMemberStatLeaderboardSorter sortBy,
        EOrder order) => sortBy switch
    {
        EMemberStatLeaderboardSorter.Points => query.OrderBy(order, x => x.Points)
            .ThenBy(order, x => x.PlayerId),
        EMemberStatLeaderboardSorter.PassCount => query.OrderBy(order, x => x.PassCount)
            .ThenBy(order, x => x.PlayerId),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };

    public static IQueryable<RankedScore> ApplySortOrder(
        this IQueryable<RankedScore> query,
        LeaderboardRequests.ERankedMapLeaderboardSorter sortBy,
        EOrder order) => sortBy switch
    {
        LeaderboardRequests.ERankedMapLeaderboardSorter.Points => query
            .OrderBy(order, x => x is ScoredRankedScore ? 0 : 1)
            .ThenBy(order, x => x is ScoredRankedScore ? ((ScoredRankedScore)x).RawPoints : default)
            .ThenBy(order, x => x is PointGivingRankedScore ? ((PointGivingRankedScore)x).Rank : 0)
            .ThenBy(order, x => x.Id),
        LeaderboardRequests.ERankedMapLeaderboardSorter.EffectiveScore => query.OrderBy(order, x => x.EffectiveScore)
            .ThenBy(order, x => x is PointGivingRankedScore ? ((PointGivingRankedScore)x).Rank : 0)
            .ThenBy(order, x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}