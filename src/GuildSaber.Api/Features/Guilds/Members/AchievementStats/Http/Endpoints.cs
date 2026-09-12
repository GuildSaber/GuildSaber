using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http.AchievementStatResponses;

namespace GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;

public class AchievementStatEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("contexts/{contextId}/members")
            .WithTag("Context.Members.AchievementStats",
                description: "Endpoints for accessing context member achievement statistics");

        group.MapGet("/{playerId}/achievement-stats", GetMemberAchievementStatsAsync)
            .WithName("GetMemberAchievementStats")
            .WithSummary("Get achievement stats for a guild member.")
            .WithDescription("Get achievement statistics for a specific player within a context.");

        group.MapGet("/@me/achievement-stats", GetCurrentMemberAchievementStatsAsync)
            .WithName("GetCurrentMemberAchievementStats")
            .WithSummary("Get achievement stats for the current authenticated member.")
            .WithDescription("Get achievement statistics for the current authenticated player within a context.")
            .RequireAuthorization();
    }

    public static async Task<Ok<List<MemberAchievementStat>>>
        GetCurrentMemberAchievementStatsAsync(
            ContextId contextId,
            ServerDbContext dbContext,
            ClaimsPrincipal claimsPrincipal)
        => await GetMemberAchievementStatsAsync(contextId, claimsPrincipal.GetPlayerId()!.Value, dbContext);

    public static async Task<Ok<List<MemberAchievementStat>>> GetMemberAchievementStatsAsync(
        ContextId contextId,
        PlayerId playerId,
        ServerDbContext dbContext)
        => TypedResults.Ok(await dbContext.MemberAchievementStats
            .AsExpandable()
            .Where(x =>
                x.ContextId == contextId &&
                x.PlayerId == playerId)
            .OrderBy(x => x.Achievement.ProgressionOrder == null)
            .ThenBy(x => x.Achievement.ProgressionOrder)
            .ThenBy(x => x.Achievement.CategoryId)
            .ThenBy(x => x.Id)
            .Select(AchievementStatMappers.MapMemberAchievementStatExpression)
            .ToListAsync()
        );
}