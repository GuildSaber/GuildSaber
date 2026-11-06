using System.Runtime.CompilerServices;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedScores.Pipelines;

public static class RankedScoreUpdateRankPipeline
{
    private static readonly string _updateAllowedRankedScoresRankFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.RankedScores)}}" rs
          SET "{{nameof(PointGivingRankedScore.Rank)}}" = subquery."NewRank"
          FROM (
              SELECT 
                  "{{nameof(RankedScore.Id)}}", 
                  DENSE_RANK() OVER (
                      PARTITION BY "{{nameof(RankedScore.RankedMapId)}}", "{{nameof(RankedScore.PointId)}}"
                      ORDER BY "{{nameof(ScoredRankedScore.RawPoints)}}" DESC, "{{nameof(RankedScore.EffectiveScore)}}" DESC
                  ) AS "NewRank"
              FROM "{{nameof(ServerDbContext.RankedScores)}}"
              WHERE "{{nameof(RankedScore.RankedMapId)}}" = {0}
                  AND ({1} = 0 OR "{{nameof(RankedScore.PointId)}}" = {1})
                  AND "{{nameof(RankedScore.IsSelected)}}"
                  AND "{{nameof(RankedScore.Type)}}" IN ({{(int)RankedScore.ERankedScoreType.Valid}}, {{(int)RankedScore.ERankedScoreType.Accepted}})
          ) subquery
          WHERE rs."{{nameof(RankedScore.Id)}}" = subquery."{{nameof(RankedScore.Id)}}"
              AND rs."{{nameof(PointGivingRankedScore.Rank)}}" IS DISTINCT FROM subquery."NewRank"
          """;

    /// <summary>
    /// Updates the ranks for all allowed ranked scores on given ranked maps.
    /// </summary>
    /// <param name="rankedMapIds">
    /// The ranked map IDs to update the ranks for.
    /// </param>
    /// <param name="dbContext">
    /// The database context to use for the operation.
    /// </param>
    /// <param name="pointId">Limits the rank update to one point when supplied.</param>
    /// <param name="token">Cancels lock acquisition and rank updates.</param>
    public static async ValueTask UpdateRanksForRankedMapsAsync(
        IEnumerable<RankedMapId> rankedMapIds,
        ServerDbContext dbContext, Point.PointId? pointId = null, CancellationToken token = default)
    {
        foreach (var rankedMapId in rankedMapIds.Distinct().OrderBy(x => x.Value))
            await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
                (dbContext, rankedMapId, pointId),
                static async (state, cancellationToken) =>
                {
                    await using var transaction = await state.dbContext.Database
                        .BeginTransactionAsync(cancellationToken);

                    await LockMapsAsync([state.rankedMapId], state.dbContext, cancellationToken);
                    await UpdateLockedMapRanksAsync(
                        [state.rankedMapId], state.dbContext, state.pointId, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }, token);
    }

    /// <summary>Serializes score reads and writes per map across workers and API instances.</summary>
    /// <remarks>Acquire every affected map in the same order, before reading candidate scores.</remarks>
    internal static async Task LockMapsAsync(
        IEnumerable<RankedMapId> rankedMapIds, ServerDbContext dbContext, CancellationToken token)
    {
        if (dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Map score locks require an active transaction.");

        foreach (var rankedMapId in rankedMapIds.Distinct().OrderBy(x => x.Value))
            // Map IDs are positive bigint values; reserve negative advisory keys for their score writes.
            await dbContext.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock({-rankedMapId.Value})", token);
    }

    internal static async Task UpdateLockedMapRanksAsync(
        IEnumerable<RankedMapId> rankedMapIds, ServerDbContext dbContext, Point.PointId? pointId,
        CancellationToken token)
    {
        foreach (var rankedMapId in rankedMapIds)
            await dbContext.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                _updateAllowedRankedScoresRankFormattableString,
                rankedMapId.Value, pointId?.Value ?? 0
            ), token);
    }
}
