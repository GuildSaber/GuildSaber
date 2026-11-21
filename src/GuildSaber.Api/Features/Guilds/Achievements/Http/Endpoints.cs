using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Guilds.Achievements.Services;
using GuildSaber.Api.Shared;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
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

        var group = endpoints.MapGroup("contexts/{contextId}/achievements")
            .WithTag("Context.Achievements", description: "Endpoints for managing achievements within a context.");

        group.MapGet("/", GetAchievementsAsync)
            .WithName("GetAchievements")
            .WithSummary("Get all achievements in a context, optionally filtered by category.")
            .WithDescription("""
                             - No parameters: Returns all achievements.
                             - hasCategory=false: Only return achievements with no category.
                             - categoryId=5: Returns achievements with category 5.
                             """);

        group.MapPost("/", CreateAchievementAsync)
            .WithName("CreateAchievement")
            .WithSummary("Create an achievement.")
            .WithDescription(
                "Create a difficulty-star, accuracy-star or map-list achievement in this context. " +
                "Creation queues a member-stat refresh. " +
                "Map-list associations are added through ranked-map proposals. " +
                "Star upper bounds are exclusive. An omitted upper bound is unbounded.")
            .ProducesValidationProblem()
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);

        group.MapPut("/{achievementId}", UpdateAchievementAsync)
            .WithName("UpdateAchievement")
            .WithSummary("Update an achievement.")
            .WithDescription(
                "Update an achievement in its current context. " +
                "Rule and progression changes queue a member-stat refresh. " +
                "The completion rule must match the existing achievement type. " +
                "Star upper bounds are exclusive. An omitted upper bound is unbounded.")
            .ProducesValidationProblem()
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);

        group.MapPut("/{achievementId}/cover", UploadAchievementCoverAsync)
            .WithName("UploadAchievementCover")
            .WithSummary("Upload an achievement cover.")
            .WithDescription("Replace the achievement cover using the form field `file`. Upload a square static PNG "
                             + $"up to 2 MiB and {ImageUploadLimits.MaxIconDimension} pixels per side.")
            .WithImageUpload()
            .RequireGuildPermission(EPermission.RankingTeam);

        group.MapDelete("/{achievementId}", DeleteAchievementAsync)
            .WithName("DeleteAchievement")
            .WithSummary("Delete an achievement.")
            .WithDescription(
                "Delete an achievement and its stored member progress and ranked-map associations. " +
                "Remaining level progression refreshes in the background. " +
                "Proposal history retains its achievement snapshots.")
            .RequireAnyGuildPermission(EPermission.RankingTeam | EPermission.GuildLeader);
    }

    private static async Task<Results<NotFound, Results<NoContent, ProblemHttpResult>>> UploadAchievementCoverAsync(
        ContextId contextId, int achievementId, IFormFile file, ServerDbContext dbContext,
        AchievementStorage storage, CancellationToken token)
        => await dbContext.Achievements.AnyAsync(x => x.ContextId == contextId && x.Id == achievementId, token) switch
        {
            false => TypedResults.NotFound(),
            true => await file.WithValidatedImageAsync(
                SKEncodedImageFormat.Png, ImageUploadLimits.MaxIconDimension, (1, 1), (storage, achievementId),
                static (state, png, token) => state.storage.UploadCoverAsync(state.achievementId, png, token), token)
        };

    private static async Task<Results<NoContent, NotFound>> DeleteAchievementAsync(
        ContextId contextId, int achievementId, AchievementService service, CancellationToken token)
        => await service.DeleteAsync(contextId, achievementId, token)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<CreatedAtRoute<Achievement>, NotFound, ValidationProblem>>
        CreateAchievementAsync(
            ContextId contextId, AchievementRequests.Definition request, AchievementService service,
            CancellationToken token)
        => await service.CreateAsync(contextId, request, token) switch
        {
            { IsFailure: true, Error: var error } => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["achievement"] = [error] }),
            { Value: null } => TypedResults.NotFound(),
            { Value: var achievement } => TypedResults.CreatedAtRoute(achievement, "GetAchievement",
                new { achievementId = achievement.Id })
        };

    private static async Task<Results<Ok<Achievement>, NotFound, ValidationProblem>> UpdateAchievementAsync(
        ContextId contextId, int achievementId, AchievementRequests.Definition request,
        AchievementService service, CancellationToken token)
        => await service.UpdateAsync(contextId, achievementId, request, token) switch
        {
            { IsFailure: true, Error: var error } => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["achievement"] = [error] }),
            { Value: null } => TypedResults.NotFound(),
            { Value: var achievement } => TypedResults.Ok(achievement)
        };

    private static async Task<Results<Ok<Achievement>, NotFound>> GetAchievementAsync(
        int achievementId,
        ServerDbContext dbContext)
        => await dbContext.Achievements
                .Where(x => x.Id == achievementId)
                .Select(x => x.Map())
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
            .Select(x => x.Map())
            .ToArrayAsync());
    }
}
