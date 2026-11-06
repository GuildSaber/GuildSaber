using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Scores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Scores.Pipelines;

public class ScoreDeletionPipeline(ServerDbContext dbContext, PointCurveService curves)
{
    public async Task ExecuteAsync(AbstractScore score, CancellationToken token = default)
    {
        var (deleted, affected) = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            (dbContext, score.Id, score.SongDifficultyId),
            static async (state, token) =>
            {
                var maps = await state.dbContext.RankedMaps
                    .Where(x =>
                        x.MapVersions.Any(v => v.SongDifficultyId == state.SongDifficultyId)
                        || x.RankedScores.Any(s => s.ScoreId == state.Id || s.PrevScoreId == state.Id))
                    .Select(x => x.Id)
                    .ToArrayAsync(token);

                await using var transaction = await state.dbContext.Database.BeginTransactionAsync(token);
                await RankedScoreUpdateRankPipeline.LockMapsAsync(maps, state.dbContext, token);

                // Both relationships cascade. Read the affected rows after other score writers finish.
                var entries = await state.dbContext.RankedScores
                    .Where(x => x.ScoreId == state.Id || x.PrevScoreId == state.Id)
                    .Select(x => new { x.RankedMapId, x.PointId, x.PlayerId })
                    .Distinct()
                    .ToArrayAsync(token);

                var deleted = await state.dbContext.Scores
                    .Where(x => x.Id == state.Id)
                    .ExecuteDeleteAsync(token);

                await transaction.CommitAsync(token);
                return (deleted, entries);
            }, token);

        if (deleted == 0) return;

        foreach (var item in affected)
            await curves.QueueRecalculationAsync(item.RankedMapId, item.PointId, true, item.PlayerId, token);
    }
}