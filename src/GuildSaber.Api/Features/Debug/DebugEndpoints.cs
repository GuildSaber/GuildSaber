using System.Security.Claims;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Debug;

public class DebugEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/debug")
            .WithTag("Debug", "Endpoints for debugging and testing purposes");

        group.MapPost("/import-old-gs-maps/{guildId}", ImportOldGuildSaberMapsBackgroundAsync)
            .WithName("ImportOldGuildSaberMapsBackground")
            .WithSummary("Import ranked maps from old GuildSaber")
            .WithDescription("Import ranked maps from old GuildSaber for a specific guild.")
            .Produces<IResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireManager();

        group.MapPost("/import-beatleader-scores/{playerId}", EnqueueBeatLeaderPlayerScoresImportAsync)
            .WithSummary("Enqueue BeatLeader player scores import.")
            .WithDescription("Queues a background task to import all scores for the specified player from BeatLeader.")
            .RequireManager();

        group.MapPost("/import-scoresaber-scores/{playerId}", EnqueueScoreSaberPlayerScoresImportAsync)
            .WithSummary("Enqueue ScoreSaber player scores import.")
            .WithDescription("Queues a background task to import all scores for the specified player from ScoreSaber.")
            .RequireManager();

        group.MapPost("/remove-all-scores", async (ServerDbContext dbContext) =>
            {
                await dbContext.Scores.ExecuteDeleteAsync();
                return TypedResults.Ok();
            })
            .WithSummary("Remove all scores from the database.")
            .WithDescription("Deletes all ranked scores from the database. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/remove-all-ranked-scores", async (ServerDbContext dbContext) =>
            {
                await dbContext.RankedScores.ExecuteDeleteAsync();
                return TypedResults.Ok();
            })
            .WithSummary("Remove all ranked scores from the database.")
            .WithDescription("Deletes all ranked scores from the database. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/remove-all-ranked-maps", async (ServerDbContext dbContext) =>
            {
                await dbContext.RankedMaps.ExecuteDeleteAsync();
                return TypedResults.Ok();
            })
            .WithSummary("Remove all ranked maps from the database.")
            .WithDescription("Deletes all ranked maps from the database. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/recalculate-member-points/{playerId}", RecalculateMemberPointStats)
            .WithSummary("Recalculate member points for a player.")
            .WithDescription("Recalculates member points for all contexts the player is a member of.")
            .RequireManager();

        group.MapPost("/recalculate-member-levels/{playerId}", RecalculateMemberLevelStats)
            .WithSummary("Recalculate member levels for a player.")
            .WithDescription("Recalculates member levels for all contexts the player is a member of.")
            .RequireManager();

        group.MapPost("/recalculate-player-scores/{playerId}", RecalculatePlayerScores)
            .WithSummary("Recalculate player scores.")
            .WithDescription("Recalculates all player scores for the specified player.")
            .RequireManager();

        group.MapPost("/recalculate-all-player-scores", RecalculateAllPlayerScores)
            .WithSummary("Recalculate all player scores.")
            .WithDescription("Recalculates all player scores for all players in the database. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/delete-member-point-stats/{playerId}", async (PlayerId playerId, ServerDbContext dbContext) =>
            {
                await dbContext.MemberPointStats
                    .Where(x => x.PlayerId == playerId)
                    .ExecuteDeleteAsync();
                TypedResults.Ok();
            }).WithSummary("Delete all member point stats for a player.")
            .WithDescription("Deletes all member point stats for the specified player. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/import-admin-conf-states", ImportAdminConfStatesAtMe)
            .WithSummary("Import admin confirmation states for the current player from legacy GuildSaber.")
            .WithDescription("Imports admin confirmation states for all pending ranked scores of the current player" +
                             " from the legacy GuildSaber system.")
            .RequireAuthorization();
    }

    private static async Task<Ok> ImportAdminConfStatesAtMe(
        ClaimsPrincipal principal,
        IBackgroundTaskQueue taskQueue,
        ServerDbContext efContext,
        IServiceScopeFactory serviceScopeFactory)
    {
        var playerId = principal.GetPlayerId()!.Value;
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LegacyGSImportAdminConfPipeline>()
                .ExecuteAsync(playerId, token);
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok> RecalculatePlayerScores(
        PlayerId playerId, ServerDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();

            await pipeline.RecalculatePlayerScoresAsync(playerId, token);
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok> RecalculateAllPlayerScores(
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            var pipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();

            await foreach (var playerId in dbContext.Players.Select(x => x.Id)
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
                await pipeline.RecalculatePlayerScoresAsync(playerId, token);
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok> RecalculateMemberPointStats(
        PlayerId playerId, ServerDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        var contextIds = await dbContext.ContextMembers.Where(x => x.PlayerId == playerId)
            .Select(x => x.ContextId)
            .ToListAsync();
        var contextsWithPoints = await dbContext.Contexts.Where(x => contextIds.Contains(x.Id) && x.Points.Any())
            .Include(x => x.Points)
            .ToListAsync();

        await taskQueue.QueueBackgroundWorkItemAsync(async _ =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var memberPointStatsPipeline = scope.ServiceProvider.GetRequiredService<MemberPointStatsPipeline>();

            foreach (var context in contextsWithPoints)
                await memberPointStatsPipeline.ExecuteAsync(playerId, context);
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok> RecalculateMemberLevelStats(
        PlayerId playerId, ServerDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        var contextIds = await dbContext.ContextMembers.Where(x => x.PlayerId == playerId)
            .Select(x => x.ContextId)
            .ToListAsync();
        var contextsWithPoints = await dbContext.Contexts.Where(x => contextIds.Contains(x.Id) && x.Points.Any())
            .Include(x => x.Points)
            .ToListAsync();

        await taskQueue.QueueBackgroundWorkItemAsync(async _ =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var memberLevelStatsPipeline = scope.ServiceProvider.GetRequiredService<MemberLevelStatsPipeline>();

            foreach (var context in contextsWithPoints)
                await memberLevelStatsPipeline.ExecuteAsync(playerId, context.GuildId, context.Id,
                    context.Points.FirstOrDefault()?.Id ?? default);
        });

        return TypedResults.Ok();
    }

    public static async Task<Results<Accepted, NotFound<string>>> EnqueueBeatLeaderPlayerScoresImportAsync(
        PlayerId playerId,
        ServerDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Where(x => x.Id == playerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (player is null)
            return TypedResults.NotFound($"Player with ID {playerId} not found.");

        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>()
                .ImportBeatLeaderScoresAsync(playerId, player.LinkedAccounts.BeatLeaderId, token);
        });

        return TypedResults.Accepted((string?)null);
    }

    public static async Task<Results<Accepted, NotFound<string>, UnprocessableEntity<string>>>
        EnqueueScoreSaberPlayerScoresImportAsync(
            PlayerId playerId,
            ServerDbContext dbContext,
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory serviceScopeFactory,
            CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Where(x => x.Id == playerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (player is null)
            return TypedResults.NotFound($"Player with ID {playerId} not found.");

        if (player.LinkedAccounts.ScoreSaberId is null)
            return TypedResults.UnprocessableEntity("Player does not have a linked ScoreSaber account.");

        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>()
                .ImportScoreSaberScoresAsync(playerId, player.LinkedAccounts.ScoreSaberId.Value, token);
        });

        return TypedResults.Accepted((string?)null);
    }

    /// <summary>
    /// Imports ranked maps from the old GuildSaber system for a specific guild using a background task.
    /// </summary>
    /// <remarks>
    /// This endpoint queues a background task to import maps and returns immediately.
    /// The import process runs asynchronously and logs progress.
    /// Returns 202 Accepted when the import task is successfully queued.
    /// Returns 404 Not Found when the guild context doesn't exist.
    /// </remarks>
    private static async Task<Results<Accepted, NotFound<string>>> ImportOldGuildSaberMapsBackgroundAsync(
        GuildId guildId,
        ServerDbContext dbContext,
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DebugEndpoints> logger,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Contexts.AnyAsync(x => x.Id == guildId && x.GuildId == guildId, cancellationToken))
            return TypedResults.NotFound($"Guild context for guild {guildId} not found.");

        // Yes, we have the assumption that ContextId == GuildId for guild contexts here.
        var contextId = new ContextId(guildId);

        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            logger.LogInformation("Starting import of old GuildSaber maps for guild {GuildId}", guildId);

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LegacyGuildSaberMapImportPipeline>()
                .ExecuteAsync(guildId, contextId, token);

            logger.LogInformation("Completed import of old GuildSaber maps for guild {GuildId}", guildId);
        });

        return TypedResults.Accepted((string?)null);
    }
}