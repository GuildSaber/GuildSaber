using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ServerGuild = GuildSaber.Database.Models.Server.Guilds.Guild;
using static GuildSaber.Api.Features.Guilds.GuildResponses;

namespace GuildSaber.Api.Features.Guilds;

public class GuildEndpoints : IEndpoints
{
    private const string GetGuildName = "GetGuild";

    public static void AddServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<GuildService>();

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/guilds")
            .WithTag("Guilds", description: "Endpoints for managing guilds in the server.");

        group.MapGet("/", GetGuildsAsync)
            .WithName("GetGuilds")
            .WithSummary("Get all guilds paginated")
            .WithDescription("Get all guilds in the server, with optional search and sorting.");

        group.MapPost("/", CreateGuildAsync)
            .WithName("CreateGuild")
            .WithSummary("Create a guild as the current player")
            .WithDescription("Create a guild as the current player using their player id from claims.")
            .Produces<Guild>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization();

        group.MapGet("/{guildId}", GetGuildAsync)
            .WithName(GetGuildName)
            .WithSummary("Get a guild")
            .WithDescription("Get a specific guild by its Id.");

        group.MapGet("/{guildId}/extended", GetGuildExtended)
            .WithName("GetGuildExtended")
            .WithSummary("Get Guild with Extended Information")
            .WithDescription("Get a specific guild with extended information by its Id."
                             + " Which includes additional fields like categories and points.");

        group.MapDelete("/{guildId}", DeleteGuildAsync)
            .WithName("DeleteGuild")
            .WithSummary("Delete a guild")
            .WithDescription("Delete a guild by its Id.")
            .RequireManager();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteGuildAsync(GuildId guildId, ServerDbContext dbContext)
    {
        var affectedRows = await dbContext.Guilds
            .Where(x => x.Id == guildId)
            .ExecuteDeleteAsync();

        return affectedRows > 0
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Ok<PagedList<Guild>>> GetGuildsAsync(
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        string? search = null,
        GuildRequests.EGuildSorters sortBy = GuildRequests.EGuildSorters.Popularity,
        EOrder order = EOrder.Desc)
    {
        var query = dbContext.Guilds.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => ((string)x.Info.Name).Contains(search));

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(GuildMappers.MapGuildExpression)
            .ToPagedListAsync(page, pageSize)
        );
    }

    /// <inheritdoc cref="GuildService.CreateGuildAsync" />
    /// <remarks>
    /// This endpoint:
    /// <list type="bullet">
    ///     <item>Returns 201 CreatedAtRoute with guild when guild is created successfully</item>
    ///     <item>Returns 401 Unauthorized when no valid player ID is found in claims</item>
    ///     <item>Returns 400 Bad Request with validation details when input validation fails</item>
    ///     <item>Returns 400 Bad Request with validation details when guild creation requirements aren't met</item>
    ///     <item>
    ///     Returns 429 Too Many Requests when the player exceeds the maximum number of guilds they are leader for allowed
    ///     </item>
    /// </list>
    /// </remarks>
    private static async Task<IResult> CreateGuildAsync(
        GuildRequests.CreateGuild request, ClaimsPrincipal principal, GuildService service)
        => principal.GetPlayerId() switch
        {
            null => TypedResults.Unauthorized(),
            var playerId => await service.CreateGuildAsync(playerId.Value, request) switch
            {
                GuildService.CreateResponse.Success(var guild) => TypedResults
                    .CreatedAtRoute(guild.Map(), GetGuildName, new { guildId = guild.Id.Value }),
                GuildService.CreateResponse.PlayerNotFound => throw new InvalidOperationException(
                    "Player ID not found in claims. Ensure the player is authenticated."),
                GuildService.CreateResponse.TooManyGuildsAsLeader(var currentCount, var maxCount) => TypedResults
                    .Problem(
                        $"You already are GuildLeader in {currentCount} guilds, which exceeds the maximum allowed of {maxCount}.",
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many guilds"),
                GuildService.CreateResponse.ValidationFailure(var errors) => TypedResults
                    .ValidationProblem(
                        detail: "Failed to create guild due to validation errors.",
                        errors: errors),
                GuildService.CreateResponse.RequirementsFailure (var errors) => TypedResults
                    .ValidationProblem(
                        errors: errors,
                        detail: "Failed to meet one or more guild creation requirements."),
                _ => throw new InvalidOperationException("Unexpected response type from CreateGuildAsync.")
            }
        };

    private static async Task<Results<Ok<Guild>, NotFound>> GetGuildAsync(
        GuildId guildId, ServerDbContext dbContext)
        => await dbContext.Guilds.Where(x => x.Id == guildId)
                .Select(GuildMappers.MapGuildExpression)
                .FirstOrDefaultAsync()
            switch
            {
                { Id: 0 } => TypedResults.NotFound(),
                var guild => TypedResults.Ok(guild)
            };

    private static async Task<Results<Ok<GuildExtended>, NotFound>> GetGuildExtended(
        GuildId guildId, ServerDbContext dbContext)
        => await dbContext.Guilds.AsSplitQuery()
                .Where(x => x.Id == guildId)
                .Select(GuildMappers.MapGuildExtendedExpression)
                .FirstOrDefaultAsync() switch
            {
                { Guild.Id: 0 } => TypedResults.NotFound(),
                var guildExtended => TypedResults.Ok(guildExtended)
            };
}

public static class GuildExtensions
{
    public static IQueryable<ServerGuild> ApplySortOrder(
        this IQueryable<ServerGuild> query, GuildRequests.EGuildSorters sortBy, EOrder order) => sortBy switch
    {
        GuildRequests.EGuildSorters.Id => query.OrderBy(order, guild => guild.Id),
        GuildRequests.EGuildSorters.Name => query.OrderBy(order, guild => guild.Info.Name)
            .ThenBy(order, guild => guild.Id),
        GuildRequests.EGuildSorters.Popularity => query.OrderBy(order, guild => guild.Status)
            .ThenBy(order, guild => guild.RankedScores.Count / guild.Members.Count)
            .ThenBy(order, guild => guild.Id),
        GuildRequests.EGuildSorters.CreationDate => query.OrderBy(order, guild => guild.Info.CreatedAt)
            .ThenBy(order, guild => guild.Id),
        GuildRequests.EGuildSorters.MemberCount => query.OrderBy(order, guild => guild.Members.Count)
            .ThenBy(order, guild => guild.Id),
        GuildRequests.EGuildSorters.MapCount => query.OrderBy(order, guild => guild.RankedMaps.Count)
            .ThenBy(order, guild => guild.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}