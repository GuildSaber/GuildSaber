using GuildSaber.Api.Extensions;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static GuildSaber.Api.Features.Guilds.Levels.LevelResponses;

namespace GuildSaber.Api.Features.Guilds.Levels;

public class LevelEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/context/{contextId}/levels")
            .WithTag("Context.Levels", description: "Endpoints for managing levels within a context.");

        group.MapGet("/", GetLevelsAsync)
            .WithName("GetLevels")
            .WithSummary("Get all levels in a context, optionally filtered by category.")
            .WithDescription("""
                             - No parameters: Returns all levels.
                             - hasCategory=false: Returns levels with no category.
                             - category=5: Returns levels in category 5.
                             """);
    }

    public static async Task<Ok<Level[]>> GetLevelsAsync(
        ContextId contextId,
        ServerDbContext dbContext,
        int? categoryId = null,
        bool? hasCategory = null)
    {
        var query = dbContext.Levels.Where(x => x.ContextId == contextId);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);
        else if (hasCategory == false)
            query = query.Where(x => x.CategoryId == null);

        return TypedResults.Ok(await query
            .OrderBy(x => x.Id)
            .Select(LevelMappers.MapLevelExpression)
            .ToArrayAsync());
    }
}