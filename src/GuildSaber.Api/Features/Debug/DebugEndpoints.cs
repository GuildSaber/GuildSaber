using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedMaps.MapVersions;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.OldGuildSaber;
using GuildSaber.Common.Services.OldGuildSaber.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Debug;

public class DebugEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/debug")
            .WithTag("Debug", "Endpoints for debugging and testing purposes");

        group.MapGet("/import-old-gs-maps/{guildId}/stream", ImportOldGuildSaberMapsAsync)
            .WithSummary("Import ranked maps from old GuildSaber system.")
            .WithDescription("Streams import progress via SSE. Import stops on client disconnect.")
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

        group.MapPost("/recalculate-member-points/{playerId}", RecalculateMemberPoints)
            .WithSummary("Recalculate member points for a player.")
            .WithDescription("Recalculates member points for all contexts the player is a member of.")
            .RequireManager();

        group.MapPost("delete-member-point-stats/{playerId}", async (PlayerId playerId, ServerDbContext dbContext) =>
            {
                await dbContext.MemberPointStats
                    .Where(x => x.PlayerId == playerId)
                    .ExecuteDeleteAsync();
                TypedResults.Ok();
            }).WithSummary("Delete all member point stats for a player.")
            .WithDescription("Deletes all member point stats for the specified player. USE WITH CAUTION!")
            .RequireManager();
    }

    private static async Task<Ok> RecalculateMemberPoints(
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

            foreach (var context in contextsWithPoints) await memberPointStatsPipeline.ExecuteAsync(playerId, context);
        });

        return TypedResults.Ok();
    }

    private static async Task<Results<NotFound<string>, ServerSentEventsResult<RankedMapResponses.RankedMap>>>
        ImportOldGuildSaberMapsAsync(
            GuildId guildId, ServerDbContext dbContext, OldGuildSaberApi oldGuildSaberApi,
            RankedMapService rankedMapService, ILogger<DebugEndpoints> logger,
            CancellationToken cancellationToken)
    {
        if (!await dbContext.Contexts.AnyAsync(x => x.Id == guildId && x.GuildId == guildId, cancellationToken))
            return TypedResults.NotFound($"Guild context for guild {guildId} not found.");

        return TypedResults.ServerSentEvents(eventType: "import-ranked-map",
            values: ImportOldGuildSaberMapsStream(
                    guildId,
                    new ContextId(guildId),
                    dbContext,
                    oldGuildSaberApi,
                    rankedMapService,
                    logger,
                    cancellationToken)
                .Select(success => success.RankedMap.Map(success.Song, success.SongDifficulty, success.GameMode)));
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

    public static async IAsyncEnumerable<RankedMapService.CreateResponse.Success> ImportOldGuildSaberMapsStream(
        GuildId guildId,
        ContextId contextId,
        ServerDbContext dbContext,
        OldGuildSaberApi oldGuildSaberApi,
        RankedMapService rankedMapService,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken token)
    {
        const int pageSize = 10;
        var request = new OldGuildSaberApi.PaginatedRequestOptions<RankedMapsSortBy>
        {
            Page = 1,
            PageSize = pageSize,
            MaxPage = int.MaxValue,
            SortBy = RankedMapsSortBy.Weight,
            Reverse = true
        };

        var categories = await dbContext.Categories.Where(x => x.GuildId == guildId).ToListAsync(token);
        if (!(await oldGuildSaberApi.GetRankingLevelsAsync(guildId.Value)).TryGetValue(out var guildRankingLevels))
            yield break;

        if (!(await oldGuildSaberApi.GetRankingCategoriesAsync(guildId.Value)).TryGetValue(out var oldCategories))
            yield break;

        var levelDict = guildRankingLevels.ToDictionary(x => x.Id, x => x);
        foreach (var oldCategory in oldCategories)
        {
            if (categories.Any(x => x.Info.Name == oldCategory.Name))
                continue;

            var newCategory = new Category
            {
                GuildId = guildId,
                Info = new CategoryInfo
                {
                    Name = Name_2_50.CreateUnsafe(oldCategory.Name).Value,
                    Description = Description.CreateUnsafe(oldCategory.Description).Value
                }
            };
            dbContext.Categories.Add(newCategory);
            categories.Add(newCategory);
        }

        await dbContext.SaveChangesAsync(token);
        dbContext.ChangeTracker.Clear();
        var categoryDict = oldCategories
            .Join(categories, o => o.Name, n => n.Info.Name, (o, n) => (Old: o, New: n))
            .ToDictionary(x => x.Old.Id, x => x.New.Id);

        await foreach (var guildRankedMapsResult in oldGuildSaberApi.GetGuildRankedMaps(guildId.Value, request)
                           .WithCancellation(token))
        {
            if (!guildRankedMapsResult.TryGetValue(out var guildRankedMaps))
                yield break;

            foreach (var rankedMap in guildRankedMaps.Where(x => x.BeatSaverId is not null))
            foreach (var difficulty in rankedMap.Difficulties.Where(x => x.GameModeName is not null))
            {
                if (await dbContext.RankedMaps.AnyAsync(MapDifficultyIsAlreadyRankedOnGuild(
                            guildId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId!.Value, dbContext),
                        token))
                    continue;

                var level = levelDict[difficulty.LevelId];
                var createRankedMap = new RankedMapRequest.CreateRankedMap
                (
                    ManualRating: new RankedMapRequest.ManualRating(
                        DifficultyStar: level.LevelNumber,
                        AccuracyStar: null),
                    Requirements: new RankedMapRequest.RankedMapRequirements(
                        NeedConfirmation: difficulty.Requirements.HasFlag(ERequirements.NeedAdminConfirmation),
                        NeedFullCombo: difficulty.Requirements.HasFlag(ERequirements.FullCombo),
                        MaxPauseDurationSec: difficulty.Requirements.HasFlag(ERequirements.MaxPauses)
                            ? 2f
                            : null,
                        ProhibitedModifiers: ModifiersMapper.ToModifiers(difficulty.ProhibitedModifiers).Map(),
                        MandatoryModifiers: ModifiersMapper.ToModifiers(difficulty.MandatoryModifiers).Map(),
                        MinAccuracy: (int)((float)difficulty.MinScoreRequirement / difficulty.MaxScore * 100f)),
                    BaseMapVersion: new MapVersionRequests.AddMapVersion(
                        BeatSaverKey: rankedMap.BeatSaverId.Value,
                        Characteristic: difficulty.GameModeName!,
                        Difficulty: difficulty.BeatSaverDifficultyValue,
                        PlayMode: "Standard",
                        Order: 0),
                    CategoryIds: difficulty.GuildCategoryId.HasValue && difficulty.GuildCategoryId != 0
                        ? [categoryDict[difficulty.GuildCategoryId.Value]]
                        : []
                );

                var tryCount = 0;
                do
                {
                    var createResult = await rankedMapService.CreateRankedMap(contextId, createRankedMap);
                    if (createResult is RankedMapService.CreateResponse.Success success)
                    {
                        yield return success;
                        break;
                    }

                    if (createResult is RankedMapService.CreateResponse.RateLimited limited)
                    {
                        await Task.Delay(limited.RetryAfter, token);
                        continue;
                    }

                    if (createResult is not RankedMapService.CreateResponse.UnexpectedFailure failure)
                        continue;

                    logger.LogError(
                        "Failed to import ranked map {RankedMapId} difficulty {DifficultyId} for guild {GuildId}: {Error}",
                        rankedMap.MapId, difficulty.DifficultyId, guildId, failure.Message
                    );
                    break;
                } while (tryCount++ < 3);
            }
        }
    }

    private static Expression<Func<RankedMap, bool>> MapDifficultyIsAlreadyRankedOnGuild(
        GuildId guildId, EDifficulty difficulty, BeatSaverKey beatSaverKey, ServerDbContext dbContext)
        => rankedMap => rankedMap.GuildId == guildId
                        && rankedMap.MapVersions.Any(y => y.SongDifficulty.Difficulty == difficulty)
                        && rankedMap.MapVersions.Any(y => dbContext
                            .Songs.Any(z => z.Id == y.SongId && z.BeatSaverKey == beatSaverKey));
}