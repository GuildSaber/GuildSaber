using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedMaps.MapVersions;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.OldGuildSaber;
using GuildSaber.Common.Services.OldGuildSaber.Models;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Mappers.BeatLeader;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Debug;

public class DebugEndpoints : IEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/debug")
            .WithTag("Debug", "Endpoints for debugging and testing purposes");

        group.MapGet("/import-old-gs-maps/{guildId}/stream",
                async Task<Results<NotFound<string>, ServerSentEventsResult<RankedMap>>>
                (GuildId guildId, ServerDbContext dbContext, OldGuildSaberApi oldGuildSaberApi,
                 RankedMapService rankedMapService, ILogger<DebugEndpoints> logger,
                 CancellationToken cancellationToken) =>
                {
                    if (!await dbContext.GuildContexts.AnyAsync(x => x.Id == guildId && x.GuildId == guildId,
                            cancellationToken))
                        return TypedResults.NotFound($"Guild context for guild {guildId} not found.");

                    return TypedResults.ServerSentEvents(
                        values: ImportOldGuildSaberMapsStream(
                            guildId, new GuildContext.GuildContextId(guildId), dbContext, oldGuildSaberApi,
                            rankedMapService, logger, cancellationToken),
                        eventType: "import-ranked-map"
                    );
                })
            .WithSummary("Import ranked maps from old GuildSaber system.")
            .WithDescription("Streams import progress via SSE. Import stops on client disconnect.")
            .RequireManager();

        group.MapPost("/import-beatleader-scores/{playerId}", EnqueueBeatLeaderPlayerScoresImportAsync)
            .WithSummary("Enqueue BeatLeader player scores import.")
            .WithDescription("Queues a background task to import all scores for the specified player from BeatLeader.")
            .RequireManager();
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

    public static async IAsyncEnumerable<RankedMap> ImportOldGuildSaberMapsStream(
        GuildId guildId,
        GuildContext.GuildContextId contextId,
        ServerDbContext dbContext,
        OldGuildSaberApi oldGuildSaberApi,
        RankedMapService rankedMapService,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken token)
    {
        const int pageSize = 10;
        var request = new OldGuildSaberApi.PaginatedRequestOptions<RankedMapsSortBy>
        {
            Page = 100,
            PageSize = pageSize,
            MaxPage = int.MaxValue,
            SortBy = RankedMapsSortBy.Weight,
            Reverse = true
        };

        await foreach (var guildRankedMapsResult in oldGuildSaberApi.GetGuildRankedMaps(guildId.Value, request)
                           .WithCancellation(token))
        {
            if (!guildRankedMapsResult.TryGetValue(out var guildRankedMaps, out var error))
                yield break;

            var rankingLevelsResult = await oldGuildSaberApi.GetRankingLevelsAsync(guildId.Value);
            if (!rankingLevelsResult.TryGetValue(out var guildRankinglevels, out var levelError))
                yield break;

            var levelDict = guildRankinglevels.ToDictionary(x => x.Id, x => x);

            foreach (var rankedMap in guildRankedMaps.Where(x => x.BeatSaverId is not null))
            foreach (var difficulty in rankedMap.Difficulties.Where(x => x.GameModeName is not null))
            {
                if (await dbContext.RankedMaps.AnyAsync(MapDifficultyIsAlreadyRankedOnGuild(
                        guildId, difficulty.BeatSaverDifficultyValue, rankedMap.BeatSaverId!.Value, dbContext), token))
                    continue;

                var level = levelDict[difficulty.LevelId];
                var createRankedMap = new RankedMapRequest.CreateRankedMap
                (
                    ContextId: contextId,
                    ManualRating: new RankedMapRequest.ManualRating(
                        DifficultyStar: level.LevelNumber,
                        AccuracyStar: null),
                    Requirements: new RankedMapRequest.RankedMapRequirements(
                        NeedConfirmation: difficulty.Requirements.HasFlag(ERequirements.NeedAdminConfirmation),
                        NeedFullCombo: difficulty.Requirements.HasFlag(ERequirements.FullCombo),
                        MaxPauseDurationSec: difficulty.Requirements.HasFlag(ERequirements.MaxPauses)
                            ? 2f
                            : null,
                        ProhibitedModifiers: ScoreMappers.ToModifiers(difficulty.ProhibitedModifiers).Map().Unwrap(),
                        MandatoryModifiers: ScoreMappers.ToModifiers(difficulty.MandatoryModifiers).Map().Unwrap(),
                        MinAccuracy: (int)((float)difficulty.MinScoreRequirement / difficulty.MaxScore * 100f)),
                    BaseMapVersion: new MapVersionRequests.AddMapVersion(
                        BeatSaverKey: rankedMap.BeatSaverId.Value,
                        Characteristic: difficulty.GameModeName!,
                        Difficulty: difficulty.BeatSaverDifficultyValue,
                        PlayMode: "Standard",
                        Order: 0)
                );

                var tryCount = 0;
                do
                {
                    var createResult = await rankedMapService.CreateRankedMap(guildId, createRankedMap);
                    if (createResult is RankedMapService.CreateResponse.Success success)
                    {
                        yield return success.RankedMap;
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
                        rankedMap.MapId, difficulty.DifficultyId, guildId,
                        failure.Message);
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