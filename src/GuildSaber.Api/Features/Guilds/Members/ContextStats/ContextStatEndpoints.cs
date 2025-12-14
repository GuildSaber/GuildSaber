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

    public static Task<Results<Ok<ContextStatResponses.MemberContextStat>, NotFound>>
        GetCurrentMemberContextStatsAsync(
            ContextId contextId,
            ServerDbContext dbContext,
            ClaimsPrincipal claimsPrincipal)
        => GetMemberContextStatsAsync(contextId, claimsPrincipal.GetPlayerId()!.Value, dbContext);

    public static async Task<Results<Ok<ContextStatResponses.MemberContextStat>, NotFound>> GetMemberContextStatsAsync(
        ContextId contextId, PlayerId playerId, ServerDbContext dbContext)
    {
        var contextStat = new ContextStatResponses.MemberContextStat
        {
            PassCountWithRank = await dbContext.MemberPointStats
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId && x.CategoryId == null)
                .OrderBy(x => x.PassCount)
                .Select(x => new ContextStatResponses.PassCountWithRank
                {
                    PassCount = x.PassCount,
                    Rank = dbContext.MemberPointStats
                        .Count(y => y.ContextId == contextId
                                    && y.CategoryId == null && y.PointId == x.PointId
                                    && y.PassCount > x.PassCount) + 1
                }).FirstOrDefaultAsync(),
            SimplePointsWithRank = await dbContext.MemberPointStats
                .Where(x => x.PlayerId == playerId && x.ContextId == contextId)
                .Select(x => new ContextStatResponses.SimplePointWithRank
                {
                    PointId = x.PointId,
                    CategoryId = x.CategoryId,
                    Points = x.Points,
                    Name = x.Point.Info.Name,
                    Rank = dbContext.MemberPointStats
                        .Count(y => y.ContextId == contextId
                                    && y.CategoryId == x.CategoryId
                                    && y.PointId == x.PointId
                                    && y.Points > x.Points) + 1
                }).ToArrayAsync()
        };

        return contextStat.PassCountWithRank == default
            ? TypedResults.NotFound()
            : TypedResults.Ok(contextStat);
    }
}