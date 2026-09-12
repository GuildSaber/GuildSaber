using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.Api.Features.Guilds.Achievements.Http;

public class AchievementEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("achievements")
            .WithTag("Achievements", description: "Endpoints for accessing achievements.")
            .MapGet("/{achievementId}", GetAchievementAsync)
            .WithName("GetAchievement")
            .WithSummary("Get an achievement.")
            .WithDescription("Get an achievement by its ID.");

        endpoints.MapGroup("contexts/{contextId}/achievements")
            .WithTag("Context.Achievements", description: "Endpoints for managing achievements within a context.")
            .MapGet("/", GetAchievementsAsync)
            .WithName("GetAchievements")
            .WithSummary("Get all achievements in a context, optionally filtered by category.")
            .WithDescription("""
                             - No parameters: Returns all achievements.
                             - hasCategory=false: Only return achievements with no category.
                             - categoryId=5: Returns achievements with category 5.
                             """);
    }

    private static async Task<Results<Ok<Achievement>, NotFound>> GetAchievementAsync(
        int achievementId,
        ServerDbContext dbContext)
        => await dbContext.Achievements
                .Where(x => x.Id == achievementId)
                .Select(AchievementMappers.MapAchievementExpression)
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                var achievement => TypedResults.Ok(achievement)
            };

    public static async Task<Ok<Achievement[]>> GetAchievementsAsync(
        ContextId contextId,
        ServerDbContext dbContext,
        CategoryId? categoryId = null,
        bool hasCategory = true)
    {
        var query = dbContext.Achievements.Where(x => x.ContextId == contextId);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId);
        else if (!hasCategory)
            query = query.Where(x => x.CategoryId == null);

        return TypedResults.Ok(await query
            .OrderBy(x => x.Id)
            .Select(AchievementMappers.MapAchievementExpression)
            .ToArrayAsync());
    }
}