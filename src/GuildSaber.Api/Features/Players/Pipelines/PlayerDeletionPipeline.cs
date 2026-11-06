using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Players.Pipelines;

public sealed class PlayerDeletionPipeline(
    ServerDbContext dbContext,
    IHeavyBackgroundTaskQueue queue,
    IServiceScopeFactory scopeFactory)
{
    public async Task<bool> ExecuteAsync(PlayerId playerId, CancellationToken token)
    {
        var (deleted, affected) = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (dbContext, playerId),
            static async (state, cancellationToken) =>
            {
                var maps = await state.dbContext.RankedMaps
                    .Where(x =>
                        x.MapVersions.Any(v => state.dbContext.Scores.Any(s =>
                            s.SongDifficultyId == v.SongDifficultyId && s.PlayerId == state.playerId))
                        || x.RankedScores.Any(s => s.PlayerId == state.playerId
                                                   || s.PrevScore != null && s.PrevScore.PlayerId == state.playerId))
                    .Select(x => x.Id)
                    .ToArrayAsync(cancellationToken);

                await using var transaction = await state.dbContext.Database.BeginTransactionAsync(cancellationToken);
                await RankedScoreUpdateRankPipeline.LockMapsAsync(maps, state.dbContext, cancellationToken);

                // The cascade removes the ranked rows identifying the maps that need a refresh.
                var entries = await state.dbContext.RankedScores
                    .Where(x =>
                        x.PlayerId == state.playerId
                        || x.Score.PlayerId == state.playerId
                        || x.PrevScore != null && x.PrevScore.PlayerId == state.playerId)
                    .Select(x => new { x.RankedMapId, x.PointId })
                    .Distinct()
                    .ToArrayAsync(cancellationToken);

                var count = await state.dbContext.Players
                    .Where(x => x.Id == state.playerId)
                    .ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return (count, entries);
            }, token);

        if (deleted == 0) return false;
        if (affected.Length == 0) return true;

        await queue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mapCurvePipeline = scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>();

            foreach (var item in affected)
                await mapCurvePipeline.ExecuteAsync(item.RankedMapId, item.PointId, true, token: cancellationToken);
        }, token);

        return true;
    }
}