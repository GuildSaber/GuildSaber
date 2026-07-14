using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedMaps.Pipelines;

public class EditRankedMapPipeline(
    ServerDbContext dbContext,
    ScoreAddOrUpdatePipeline scoreAddOrUpdatePipeline,
    ILogger<EditRankedMapPipeline> logger)
{
    public async Task ExecuteAsync(RankedMapId rankedMapId, CancellationToken token)
    {
        logger.LogInformation("Executing edit pipeline for ranked map with Id {RankedMapId}", rankedMapId);

        var versions = await dbContext.MapVersions
            .Where(x => x.RankedMapId == rankedMapId)
            .Select(x => x.SongDifficultyId)
            .ToArrayAsync(token);

        if (versions.Length == 0) return;
        await foreach (var score in dbContext.Scores
                           .Where(x => ((IEnumerable<SongDifficultyId>)versions).Contains(x.SongDifficultyId))
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
            await scoreAddOrUpdatePipeline.ExecuteAsync(score, token);

        logger.LogInformation("Completed editing ranked map with Id {RankedMapId}", rankedMapId);
    }
}