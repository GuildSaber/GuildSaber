using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.HttpResults.ResultExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using CategoryId = GuildSaber.Database.Models.Server.Guilds.Categories.Category.CategoryId;
using ServerCategory = GuildSaber.Database.Models.Server.Guilds.Categories.Category;
using ServerCategoryInfo = GuildSaber.Database.Models.Server.Guilds.Categories.CategoryInfo;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

namespace GuildSaber.Api.Features.Guilds.Categories;

public class CategoryEndpoints : IEndpoints
{
    public const string GetCategoryName = "GetCategory";

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var categoriesGroup = endpoints.MapGroup("categories")
            .WithTag("Categories", description: "Endpoints for accessing categories in the server.");

        categoriesGroup.MapGet("/", GetCategoriesPaginatedAsync)
            .WithName("GetCategoriesPaginated")
            .WithSummary("Get all categories paginated.")
            .WithDescription("Get all categories in the server.");

        categoriesGroup.MapGet("/{categoryId}", GetCategoryAsync)
            .WithName(GetCategoryName)
            .WithSummary("Get a category.")
            .WithDescription("Get a category in the server by its Id.");

        var guildsGroup = endpoints.MapGroup("guilds/{guildId}/categories")
            .WithTag("Guilds.Categories", description: "Endpoints for managing guild categories by guild id.");

        guildsGroup.MapGet("/", GetCategoriesFromGuildAsync)
            .WithName("GetCategories")
            .WithSummary("Get all categories of a guild.")
            .WithDescription("Get all categories of a guild by guild id.");

        guildsGroup.MapPost("/", CreateCategoryAsync)
            .WithName("CreateCategory")
            .WithSummary("Create a category for a guild.")
            .WithDescription("Create a category for a guild by its Id.")
            .ProducesProblem(statusCode: StatusCodes.Status400BadRequest)
            .RequireGuildPermission(EPermission.RankingTeam);

        guildsGroup.MapPut("/{categoryId}", UpdateCategoryAsync)
            .WithName("UpdateCategory")
            .WithSummary("Update a category of a guild.")
            .WithDescription("Update a category of a guild by its Id.")
            .RequireGuildPermission(EPermission.RankingTeam);

        guildsGroup.MapDelete("/{categoryId}", DeleteCategoryAsync)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category of a guild.")
            .WithDescription("Delete a category of a guild by its Id.")
            .RequireGuildPermission(EPermission.RankingTeam);
    }

    private static async Task<Results<Ok<Category>, NotFound>> GetCategoryAsync(
        CategoryId categoryId, ServerDbContext dbContext)
        => await dbContext.Categories.Where(x => x.Id == categoryId)
                .Select(CategoryMappers.MapCategoryExpression)
                .Cast<Category?>()
                .FirstOrDefaultAsync() switch
            {
                null => TypedResults.NotFound(),
                var category => TypedResults.Ok(category.Value)
            };

    private static async Task<Ok<PagedList<Category>>> GetCategoriesPaginatedAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10)
        => TypedResults.Ok(await dbContext.Categories
            .OrderBy(x => x.Id)
            .Select(CategoryMappers.MapCategoryExpression)
            .ToPagedListAsync(page, pageSize));

    private static async Task<Ok<Category[]>> GetCategoriesFromGuildAsync(
        GuildId guildId, ServerDbContext dbContext)
        => TypedResults.Ok(await dbContext.Categories
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Id)
            .Select(CategoryMappers.MapCategoryExpression)
            .ToArrayAsync());

    private static async Task<Results<CreatedAtRoute<Category>, ProblemHttpResult>>
        CreateCategoryAsync(GuildId guildId, CategoryRequests.CreateCategory request, ServerDbContext dbContext)
        => await (from name in Name_2_50.TryCreate(request.Name)
                  from description in Description.TryCreate(request.Description)
                  select new ServerCategory { GuildId = guildId, Info = new ServerCategoryInfo(name, description) })
            .Map(static (category, dbContext) => dbContext
                .AddAndSaveAsync(category, x => x.Map()), dbContext)
            .ToCreatedAtRouteHttpResult(
                GetCategoryName,
                res => new { guildId, categoryId = res.Id }
            );

    private static async Task<Results<Ok<Category>, ProblemHttpResult>> UpdateCategoryAsync(
        GuildId guildId, CategoryId categoryId, CategoryRequests.UpdateCategory request,
        ServerDbContext dbContext)
        => await (from name in Name_2_50.TryCreate(request.Name)
                  from description in Description.TryCreate(request.Description)
                  select new ServerCategory
                      { Id = categoryId, GuildId = guildId, Info = new ServerCategoryInfo(name, description) })
            .Map(static (category, dbContext) => dbContext
                .UpdateAndSaveAsync(category, x => x.Map()), dbContext)
            .ToOkHttpResult();

    private static async Task<Results<NoContent, NotFound>> DeleteCategoryAsync(
        GuildId guildId, CategoryId categoryId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Categories
            .Where(x => x.GuildId == guildId && x.Id == categoryId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}