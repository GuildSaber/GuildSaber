using System.Diagnostics;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Scores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Scores.Pipelines;

public class ScoreDeletionPipeline(ServerDbContext dbContext)
{
    private readonly record struct GuildsWithRankedMaps(GuildId[] GuildIds, RankedMapId[] RankedMapIds);

    public async Task ExecuteAsync(AbstractScore score)
        => await ExecuteAsync(score, await GetImpactedGuildsAndRankedMapsFromScoreIdsIfDeletionAsync(score.Id));

    private async ValueTask ExecuteAsync(AbstractScore score, GuildsWithRankedMaps guildsWithRankedMaps)
    {
        var affectedRow = await dbContext.Scores.Where(x => x.Id == score.Id).ExecuteDeleteAsync();
        Trace.Assert(!(guildsWithRankedMaps.RankedMapIds.Length != 0 && affectedRow == 0),
            "If there were ranked scores, the score should have existed and been deleted.");

        if (affectedRow == 0) return;
        if (guildsWithRankedMaps.RankedMapIds.Length == 0) return;

        //TODO: Recalculate rankedScores for each RankedMapIds because one of their scores got deleted, and there might be a score that fits a new ranked score. (UpdateOrAddAsync should just do it too, yoink the call from there)
        await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync(guildsWithRankedMaps.RankedMapIds, dbContext);
    }

    /// <summary>
    /// Retrieves the impacted guilds and ranked maps associated with a score ID, if any ranked scores exist for that score ID.
    /// </summary>
    /// <param name="scoreId"></param>
    private async Task<GuildsWithRankedMaps> GetImpactedGuildsAndRankedMapsFromScoreIdsIfDeletionAsync(ScoreId scoreId)
        => new(
            await dbContext.RankedScores
                .Where(x => x.ScoreId == scoreId)
                .Select(x => x.GuildId)
                .Distinct()
                .ToArrayAsync(),
            await dbContext.RankedScores
                .Where(x => x.ScoreId == scoreId)
                .Select(x => x.RankedMapId)
                .ToArrayAsync()
        );
}