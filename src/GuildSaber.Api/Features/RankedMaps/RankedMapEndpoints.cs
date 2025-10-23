using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using static GuildSaber.Api.Features.RankedMaps.RankedMapService;

namespace GuildSaber.Api.Features.RankedMaps;

public class RankedMapEndpoints : IEndpoints
{
    public static void AddServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<RankedMapService>();

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("guilds/{guildId}/ranked-maps")
            .WithTag("Guilds.RankedMaps", description: "Endpoints for managing guild ranked maps by guild id.");

        group.MapPost("/", CreateRankedMapAsync)
            .WithName("CreateRankedMap")
            .WithSummary("Create a ranked map for a guild.")
            .WithDescription("Create a ranked map for a guild by its Id.")
            .Produces<RankedMap>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem()
            .RequireGuildPermission(EPermission.RankingTeam);
    }

    /// <inheritdoc cref="RankedMapService.CreateRankedMap" />
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
        GuildId guildId,
        RankedMapRequest.CreateRankedMap create,
        RankedMapService rankedMapService)
        => await rankedMapService.CreateRankedMap(guildId, create) switch
        {
            CreateResponse.Success(var rankedMap) => TypedResults
                .Ok(rankedMap),
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
}