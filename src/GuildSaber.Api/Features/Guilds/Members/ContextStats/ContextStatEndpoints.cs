using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.ContextStats;

public class ContextStatEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/contexts/{contextId}/members")
            .WithTag("Context.Members.ContextStats",
                description: "Endpoints for accessing context member statistics");

        group.MapGet("/{playerId}/context-stats", GetMemberContextStatsAsync)
            .WithName("GetMemberContextStats")
            .WithSummary("Get context stats for a guild member.")
            .WithDescription("Get context statistics for a specific player within a context.");

        group.MapGet("/@me/context-stats", GetCurrentMemberContextStatsAsync)
            .WithName("GetCurrentMemberContextStats")
            .WithSummary("Get context stats for the current authenticated member.")
            .WithDescription("Get context statistics for the current authenticated player within a context.")
            .RequireAuthorization();
    }

    public static async Task<Results<Ok<ContextStatResponses.MemberContextStat>, NotFound>>
        GetCurrentMemberContextStatsAsync(
            ContextId contextId,
            ServerDbContext dbContext,
            ClaimsPrincipal claimsPrincipal)
        => await GetMemberContextStatsAsync(contextId, claimsPrincipal.GetPlayerId()!.Value, dbContext);

    public static async Task<Results<Ok<ContextStatResponses.MemberContextStat>, NotFound>> GetMemberContextStatsAsync(
        ContextId contextId, PlayerId playerId, ServerDbContext dbContext)
    {
        var referenceStat = await dbContext.MemberPointStats
            .Where(x => x.PlayerId == playerId && x.ContextId == contextId && x.CategoryId == null)
            .OrderBy(x => x.PointId)
            .FirstOrDefaultAsync();

        if (referenceStat == null)
            return TypedResults.NotFound();

        var contextStat = new ContextStatResponses.MemberContextStat
        {
            PassCountsWithRank = await dbContext.MemberPointStats
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .GroupBy(x => x.CategoryId)
                .Select(x => new ContextStatResponses.PassCountWithRank
                {
                    CategoryId = x.Key,
                    PassCount = x.Select(y => y.PassCount).First(),
                    Rank = dbContext.MemberPointStats.Count(others =>
                        others.ContextId == contextId
                        && others.CategoryId == x.Key
                        && others.PointId == referenceStat.PointId
                        && others.PassCount > referenceStat.PassCount) + 1
                }).ToArrayAsync(),
            SimplePointsWithRank = await dbContext.MemberPointStats
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .Select(x => new ContextStatResponses.SimplePointWithRank
                {
                    PointId = x.PointId,
                    CategoryId = x.CategoryId,
                    Points = x.Points,
                    Name = x.Point.Info.Name,
                    Rank = dbContext.MemberPointStats
                        .Count(y =>
                            y.ContextId == contextId
                            && y.CategoryId == x.CategoryId
                            && y.PointId == x.PointId
                            && y.Points > x.Points) + 1
                }).ToArrayAsync()
        };

        return TypedResults.Ok(contextStat);
    }
}