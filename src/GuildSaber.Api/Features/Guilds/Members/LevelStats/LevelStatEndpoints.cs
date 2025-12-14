using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats;

public class LevelStatEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("contexts/{contextId}/members")
            .WithTag("Context.Members.LevelStats",
                description: "Endpoints for accessing context member level statistics");

        group.MapGet("/{playerId}/level-stats", GetMemberLevelStatsAsync)
            .WithName("GetMemberLevelStats")
            .WithSummary("Get level stats for a guild member.")
            .WithDescription("Get level statistics for a specific player within a context.");

        group.MapGet("/@me/level-stats", GetCurrentMemberLevelStatsAsync)
            .WithName("GetCurrentMemberLevelStats")
            .WithSummary("Get level stats for the current authenticated member.")
            .WithDescription("Get level statistics for the current authenticated player within a context.")
            .RequireAuthorization();
    }

    public static Task<Ok<List<LevelStatResponses.MemberLevelStat>>>
        GetCurrentMemberLevelStatsAsync(
            ContextId contextId,
            ServerDbContext dbContext,
            ClaimsPrincipal claimsPrincipal)
        => GetMemberLevelStatsAsync(contextId, claimsPrincipal.GetPlayerId()!.Value, dbContext);

    public static async Task<Ok<List<LevelStatResponses.MemberLevelStat>>> GetMemberLevelStatsAsync(
        ContextId contextId,
        PlayerId playerId,
        ServerDbContext dbContext)
        => TypedResults.Ok(await dbContext.MemberLevelStats
            .Where(x =>
                x.ContextId == contextId &&
                x.PlayerId == playerId)
            .OrderBy(x => x.Level.Order)
            .Select(LevelStatMappers.MapMemberLevelStatExpression(dbContext))
            .ToListAsync()
        );
}