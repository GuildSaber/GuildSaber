using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.HttpResults.ResultExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Categories;

public class CategoryEndpoints : IEndPoints
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
            .RequireGuildPermission(Member.EPermission.RankingTeam);

        guildsGroup.MapPut("/{categoryId}", UpdateCategoryAsync)
            .WithName("UpdateCategory")
            .WithSummary("Update a category of a guild.")
            .WithDescription("Update a category of a guild by its Id.")
            .RequireGuildPermission(Member.EPermission.RankingTeam);

        guildsGroup.MapDelete("/{categoryId}", DeleteCategoryAsync)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category of a guild.")
            .WithDescription("Delete a category of a guild by its Id.")
            .RequireGuildPermission(Member.EPermission.RankingTeam);
    }

    private static async Task<Results<Ok<CategoryResponses.Category>, NotFound>> GetCategoryAsync(
        Category.CategoryId categoryId, ServerDbContext dbContext)
        => await dbContext.Categories.Where(x => x.Id == categoryId)
                .Select(CategoryMappers.MapCategoryExpression)
                .FirstOrDefaultAsync() switch
            {
                { Id: 0 } => TypedResults.NotFound(),
                var category => TypedResults.Ok(category)
            };

    private static async Task<Ok<PagedList<CategoryResponses.Category>>> GetCategoriesPaginatedAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10)
        => TypedResults.Ok(await PagedList<CategoryResponses.Category>.CreateAsync(
            dbContext.Categories.OrderBy(x => x.Id).Select(CategoryMappers.MapCategoryExpression), page, pageSize));

    private static async Task<Ok<CategoryResponses.Category[]>> GetCategoriesFromGuildAsync(
        Guild.GuildId guildId, ServerDbContext dbContext)
        => TypedResults.Ok(await dbContext.Categories
            .Where(x => x.GuildId == guildId)
            .OrderBy(x => x.Id)
            .Select(CategoryMappers.MapCategoryExpression)
            .ToArrayAsync());

    private static async Task<Results<CreatedAtRoute<CategoryResponses.Category>, ProblemHttpResult>>
        CreateCategoryAsync(Guild.GuildId guildId, CategoryRequests.CreateCategory request, ServerDbContext dbContext)
        => await (from name in Name_2_50.TryCreate(request.Name)
                  from description in Description.TryCreate(request.Description)
                  select new Category { GuildId = guildId, Info = new CategoryInfo(name, description) })
            .Map(static (category, dbContext) => dbContext
                .AddAndSaveAsync(category, x => x.Map())
                .MapError(x => x.ToString()), dbContext)
            .ToCreatedAtRouteHttpResult(
                GetCategoryName,
                res => new { guildId, categoryId = res.Id }
            );

    private static async Task<Results<Ok<CategoryResponses.Category>, ProblemHttpResult>> UpdateCategoryAsync(
        Guild.GuildId guildId, Category.CategoryId categoryId, CategoryRequests.UpdateCategory request,
        ServerDbContext dbContext)
        => await (from name in Name_2_50.TryCreate(request.Name)
                  from description in Description.TryCreate(request.Description)
                  select new Category
                      { Id = categoryId, GuildId = guildId, Info = new CategoryInfo(name, description) })
            .Map(static (category, dbContext) => dbContext
                .UpdateAndSaveAsync(category, x => x.Map())
                .MapError(x => x.ToString()), dbContext)
            .ToOkHttpResult();

    private static async Task<Results<NoContent, NotFound>> DeleteCategoryAsync(
        Guild.GuildId guildId, Category.CategoryId categoryId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Categories
            .Where(x => x.GuildId == guildId && x.Id == categoryId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}