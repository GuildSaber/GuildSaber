using System.ComponentModel.DataAnnotations;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerRankedMap = GuildSaber.Database.Models.Server.RankedMaps.RankedMap;
using RankedMapId = GuildSaber.Database.Models.Server.RankedMaps.RankedMap.RankedMapId;
using static GuildSaber.Api.Features.RankedMaps.RankedMapService;
using static GuildSaber.Api.Features.RankedMaps.RankedMapResponses;

namespace GuildSaber.Api.Features.RankedMaps;

public class RankedMapEndpoints : IEndpoints
{
    public static void AddServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<RankedMapService>();

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var rankedMapGroup = endpoints.MapGroup("ranked-maps")
            .WithTag("RankedMaps", description: "Endpoints for accessing ranked maps in the server.");

        rankedMapGroup.MapGet("/{rankedMapId}", GetRankedMapAsync)
            .WithName("GetRankedMap")
            .WithSummary("Get a ranked map.")
            .WithDescription("Get a ranked map in the server by its Id.");

        var group = endpoints.MapGroup("contexts/{contextId}/ranked-maps")
            .WithTag("Context.RankedMaps", description: "Endpoints for managing ranked maps within a context.");

        group.MapGet("/", GetRankedMapsAsync)
            .WithName("GetRankedMaps")
            .WithSummary("Get ranked maps for a context.")
            .WithDescription("Get ranked maps for a context by its Id, with optional search and sorting.");

        group.MapPost("/", CreateRankedMapAsync)
            .WithName("CreateRankedMap")
            .WithSummary("Create a ranked map for a context.")
            .WithDescription("Create a ranked map for a context by its Id.")
            .Produces<RankedMap>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem()
            .RequireGuildPermission(EPermission.RankingTeam);
    }

    /// <remarks>
    /// This endpoint:
    /// <list type="bullet">
    ///     <item>Returns 200 OK with ranked map details when created successfully.</item>
    ///     <item>Returns 401 Unauthorized when the guild has reached its maximum number of ranked maps.</item>
    ///     <item>Returns 400 Bad Request with validation details when validation fails.</item>
    ///     <item>Returns 404 Not Found when the map is not found on BeatSaver.</item>
    ///     <item>Returns 429 Too Many Requests when rate limited by BeatSaver API.</item>
    ///     <item>Returns 500 Internal Server Error when BeatSaver API fails.</item>
    /// </list>
    /// </remarks>
    public static async Task<IResult> CreateRankedMapAsync(
        ContextId contextId, RankedMapRequest.CreateRankedMap create, RankedMapService rankedMapService)
        => await rankedMapService.CreateRankedMap(contextId, create) switch
        {
            CreateResponse.Success(var rankedMap, var song, var songDifficulty, var gameMode) => TypedResults
                .Ok(rankedMap.Map(song, songDifficulty, gameMode)),
            CreateResponse.TooManyRankedMaps(var current, var max) => TypedResults.Problem(
                $"Guild has reached its maximum number of ranked maps ({current}/{max}), consider getting more boosts.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Too many ranked maps"),
            CreateResponse.ValidationFailure(var errors) => TypedResults
                .ValidationProblem(errors: errors,
                    detail: "Failed to validate ranked map creation."),
            CreateResponse.NotOnBeatSaver(var beatSaverKey) => TypedResults
                .NotFound($"Map with BeatSaver key {beatSaverKey} not found."),
            CreateResponse.RateLimited(var retryAfter) => TypedResults
                .Problem($"Rate limited by BeatSaver API. Retry after {retryAfter.TotalSeconds:N0} seconds.",
                    statusCode: StatusCodes.Status429TooManyRequests),
            CreateResponse.BeatSaverError(var message) => TypedResults
                .InternalServerError($"BeatSaver API error: {message}"),
            CreateResponse.UnexpectedFailure(var message) => TypedResults
                .InternalServerError($"Unexpected error: {message}"),
            _ => throw new ArgumentOutOfRangeException(nameof(rankedMapService.CreateRankedMap),
                "Unexpected response from CreateRankedMap.")
        };

    private static async Task<Results<Ok<RankedMap>, NotFound>> GetRankedMapAsync(
        RankedMapId rankedMapId, ServerDbContext dbContext) => await dbContext.RankedMaps
            .Where(x => x.Id == rankedMapId)
            .Select(RankedMapMappers.MapRankedMapExpression)
            .FirstOrDefaultAsync() switch
        {
            null => TypedResults.NotFound(),
            var rankedMap => TypedResults.Ok(rankedMap)
        };

    private static async Task<Ok<PagedList<RankedMap>>> GetRankedMapsAsync(
        [FromRoute] ContextId contextId,
        ServerDbContext dbContext,
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int pageSize = 10,
        string? search = null,
        RankedMapRequest.ERankedMapSorter sortBy = RankedMapRequest.ERankedMapSorter.DifficultyStar,
        EOrder order = EOrder.Asc)
    {
        var query = dbContext.RankedMaps.AsSplitQuery().Where(x => x.ContextId == contextId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (search.Length is < 10 and >= 5 && search.StartsWith("!bsr"))
            {
                search = search[5..];
                query = query.Where(x => x.MapVersions.Any(version => version.Song.BeatSaverKey == search));
            }
            else
            {
                query = query.Where(x => x.MapVersions.Any(version =>
                    EF.Functions.ILike(version.Song.Info.SongName, $"%{search}%") ||
                    EF.Functions.ILike(version.Song.Info.SongAuthorName, $"%{search}%") ||
                    EF.Functions.ILike(version.Song.Info.MapperName, $"%{search}%") ||
                    search.Length < 5 && version.Song.BeatSaverKey != null
                                      && EF.Functions.ILike(version.Song.BeatSaverKey, $"%{search}%") ||
                    search.Length > 36 && search.Length < 43 &&
                    EF.Functions.ILike(version.Song.Hash, $"%{search}%")));
            }
        }

        return TypedResults.Ok(await query
            .ApplySortOrder(sortBy, order)
            .Select(RankedMapMappers.MapRankedMapExpression)
            .ToPagedListAsync(page, pageSize));
    }
}

public static class RankedMapExtensions
{
    public static IQueryable<ServerRankedMap> ApplySortOrder(
        this IQueryable<ServerRankedMap> query, RankedMapRequest.ERankedMapSorter sortBy, EOrder order) => sortBy switch
    {
        RankedMapRequest.ERankedMapSorter.Id => query.OrderBy(order, x => x.Id),
        RankedMapRequest.ERankedMapSorter.CreationTime => query.OrderBy(order, x => x.Info.CreatedAt)
            .ThenBy(order, x => x.Id),
        RankedMapRequest.ERankedMapSorter.EditTime => query.OrderBy(order, x => x.Info.EditedAt)
            .ThenBy(order, guild => guild.Id),
        RankedMapRequest.ERankedMapSorter.DifficultyStar => query.OrderBy(order, x => x.Rating.DiffStar)
            .ThenBy(order, x => x.Id),
        RankedMapRequest.ERankedMapSorter.AccuracyStar => query.OrderBy(order, x => x.Rating.AccStar)
            .ThenBy(order, x => x.Id),
        RankedMapRequest.ERankedMapSorter.Name => query.OrderBy(order, x => x.MapVersions
                .Select(v => v.Song.Info.SongName)
                .FirstOrDefault())
            .ThenBy(order, x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
    };
}