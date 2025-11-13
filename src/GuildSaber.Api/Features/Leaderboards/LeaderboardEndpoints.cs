using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.AspNetCore.Http.HttpResults;
using RankedMapId = GuildSaber.Database.Models.Server.RankedMaps.RankedMap.RankedMapId;
using PointId = GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId;

namespace GuildSaber.Api.Features.Leaderboards;

public class LeaderboardEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contexts/{contextId}/points/{pointId}")
            .WithTag("Leaderboards", description: "Endpoints for accessing leaderboards in the server.");

        group.MapGet("/ranked-maps/{rankedMapId}/leaderboard", GetLeaderboardAsync)
            .WithName("GetContextPointRankedMapLeaderboard")
            .WithSummary("Get the leaderboard for a ranked map within a context point, paginated.")
            .WithDescription("Get the leaderboard for a ranked map within a context point by its Id, paginated.");
    }

    public static async Task<Ok<PagedList<RankedScoreResponses.RankedScoreWithPlayer>>> GetLeaderboardAsync(
        GuildId guildId,
        ContextId contextId,
        PointId pointId,
        RankedMapId rankedMapId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        LeaderboardRequests.ERankedMapLeaderboardSorter sortBy = LeaderboardRequests.ERankedMapLeaderboardSorter.Points,
        EOrder order = EOrder.Asc)
        => TypedResults.Ok(await dbContext.RankedScores
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PointId == pointId &&
                x.RankedMapId == rankedMapId)
            .ApplySortOrder(sortBy, order)
            .Select(RankedScoreMappers.MapRankedScoreWithPlayerExpression(dbContext))
            .ToPagedListAsync(page, pageSize)
        );
}

public static class LeaderboardExtensions
{
    public static IQueryable<RankedScore> ApplySortOrder(
        this IQueryable<RankedScore> query,
        LeaderboardRequests.ERankedMapLeaderboardSorter sortBy,
        EOrder order) => sortBy switch
    {
        LeaderboardRequests.ERankedMapLeaderboardSorter.Points => query.OrderBy(order, x => x.RawPoints)
            .ThenBy(order, x => x.Id),
        LeaderboardRequests.ERankedMapLeaderboardSorter.EffectiveScore => query.OrderBy(order, x => x.EffectiveScore)
            .ThenBy(order, x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}