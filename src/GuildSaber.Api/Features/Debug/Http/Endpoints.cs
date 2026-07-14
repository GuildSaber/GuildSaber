using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedMaps.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using GuildSaber.Database.Contexts.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Debug.Http;

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

        group.MapPost("/refetch-all-player-scores-from-bl-and-scoresaber", RefetchAllPlayerScoresFromBLandSS)
            .WithSummary("Refetch all player scores from BeatLeader and ScoreSaber.")
            .WithDescription(
                "Refetches all player scores for all players in the database from BeatLeader and ScoreSaber. USE WITH CAUTION!")
            .RequireManager();

        group.MapPost("/import-all-admin-conf", ImportAllAdminConf)
            .WithSummary("Import all admin confirmations from the old GuildSaber system.")
            .WithDescription(
                "Imports all admin confirmations from the old GuildSaber system for all players and guilds. USE WITH CAUTION!")
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

        group.MapPost("/trigger-edit-ranked-map-pipeline/{rankedMapId}", TriggerEditMapPipeline)
            .WithSummary("Trigger the EditRankedMapPipeline for a specific ranked map.")
            .WithDescription("Triggers the EditRankedMapPipeline for the specified ranked map")
            .RequireManager();
    }


    private readonly record struct PlayerIdWithGuildIds(PlayerId PlayerId, GuildId[] GuildIds);

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
        IHeavyBackgroundTaskQueue taskQueue,
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

    private static async Task<Ok> ImportAllAdminConf(
        ServerDbContext dbContext,
        IHeavyBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory)
    {
        var playersWithGuilds = await dbContext.Players
            .Select(p => new PlayerIdWithGuildIds(p.Id, p.Members.Select(x => x.GuildId).ToArray()))
            .ToArrayAsync();

        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var playerScoresPipeline = scope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();
            var adminConfPipeline = scope.ServiceProvider.GetRequiredService<LegacyGSImportAdminConfPipeline>();

            foreach (var playerWithGuilds in playersWithGuilds)
            {
                var importedAny = false;
                foreach (var guildId in playerWithGuilds.GuildIds)
                    importedAny |= await adminConfPipeline.ExecuteAsync(guildId, playerWithGuilds.PlayerId, token);

                if (importedAny)
                    await playerScoresPipeline.RecalculatePlayerScoresAsync(playerWithGuilds.PlayerId, token);
            }
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok> RefetchAllPlayerScoresFromBLandSS(
        IHeavyBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        PlayerId? fromPlayerId = null)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DebugEndpoints>>();
            await using var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            await foreach (var player in dbContext.Players
                               .Where(x => x.Id >= (fromPlayerId ?? 0))
                               .Select(x => new
                               {
                                   x.Id, BeatLeaderId = x.LinkedAccounts.BeatLeaderId(), x.LinkedAccounts.ScoreSaberId
                               })
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
            {
                // There were weird issues, so maybe creating a pipeline per player will help.
                await using var playerScope = scope.ServiceProvider.CreateAsyncScope();
                var pipeline = playerScope.ServiceProvider.GetRequiredService<PlayerScoresPipeline>();

                try
                {
                    await pipeline.ImportBeatLeaderScoresAsync(player.Id, player.BeatLeaderId, token);

                    if (player.ScoreSaberId is { } scoreSaberId)
                        await pipeline.ImportScoreSaberScoresAsync(player.Id, scoreSaberId, token);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error refetching scores from BL and SS for player {PlayerId}", player.Id);
                }
            }
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
                .ImportBeatLeaderScoresAsync(playerId, player.LinkedAccounts.BeatLeaderId(), token);
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
        IHeavyBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Contexts.AnyAsync(x => x.Id == guildId && x.GuildId == guildId, cancellationToken))
            return TypedResults.NotFound($"Guild context for guild {guildId} not found.");

        // Yes, we have the assumption that ContextId == GuildId for guild contexts here.
        var contextId = new ContextId(guildId);

        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LegacyGuildSaberMapImportPipeline>()
                .ExecuteAsync(guildId, contextId, token);
        });

        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Ok> TriggerEditMapPipeline(
        RankedMapId rankedMapId, IBackgroundTaskQueue taskQueue, IServiceScopeFactory serviceScopeFactory)
    {
        await taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<EditRankedMapPipeline>()
                .ExecuteAsync(rankedMapId, token);
        });

        return TypedResults.Ok();
    }
}