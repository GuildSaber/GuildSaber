using System.Runtime.CompilerServices;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedScores.Pipelines;

public static class RankedScoreUpdateRankPipeline
{
    private static readonly string _updateAllowedRankedScoresRankFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.RankedScores)}}" rs
          SET "{{nameof(RankedScore.Rank)}}" = subquery."NewRank"
          FROM (
              SELECT 
                  "{{nameof(RankedScore.Id)}}", 
                  DENSE_RANK() OVER (
                      PARTITION BY "{{nameof(RankedScore.RankedMapId)}}", "{{nameof(RankedScore.PointId)}}"
                      ORDER BY "{{nameof(RankedScore.RawPoints)}}" DESC, "{{nameof(RankedScore.EffectiveScore)}}" DESC
                  ) AS "NewRank"
              FROM "{{nameof(ServerDbContext.RankedScores)}}"
              WHERE "{{nameof(RankedScore.RankedMapId)}}" = {0}
                  AND ("{{nameof(RankedScore.State)}}" & {{(int)(RankedScore.EState.Selected | RankedScore.EState.NonPointGiving)}}) = {{(int)RankedScore.EState.Selected}}
          ) subquery
          WHERE rs."{{nameof(RankedScore.Id)}}" = subquery."{{nameof(RankedScore.Id)}}"
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
    public static async ValueTask UpdateRanksForRankedMapsAsync(
        IEnumerable<RankedMap.RankedMapId> rankedMapIds,
        ServerDbContext dbContext)
    {
        foreach (var rankedMapId in rankedMapIds)
            await dbContext.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                _updateAllowedRankedScoresRankFormattableString,
                rankedMapId.Value
            ));
    }
}