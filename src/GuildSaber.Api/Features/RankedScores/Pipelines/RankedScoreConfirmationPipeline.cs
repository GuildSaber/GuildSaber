using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.RankedScores.Pipelines;

public sealed class RankedScoreConfirmationPipeline(
    ServerDbContext dbContext,
    ScoreAddOrUpdatePipeline scoreAddOrUpdatePipeline,
    MemberPointStatsPipeline memberPointStatsPipeline,
    MemberLevelStatsPipeline memberLevelStatsPipeline,
    ILogger<RankedScoreConfirmationPipeline> logger)
{
    public async Task ExecuteAsync(ContextId contextId, RankedScoreId rankedScoreId, CancellationToken token)
    {
        logger.LogInformation(
            "Processing confirmation side effects for ranked score {RankedScoreId} in context {ContextId}.",
            rankedScoreId, contextId);

        var rankedScore = await dbContext.RankedScores
            .Include(x => x.Score)
            .Where(x => x.ContextId == contextId && x.Id == rankedScoreId)
            .FirstOrDefaultAsync(token);

        if (rankedScore is null)
        {
            logger.LogWarning(
                "Could not process confirmation side effects for ranked score {RankedScoreId} in context {ContextId}: score was not found.",
                rankedScoreId, contextId);
            return;
        }

        var result = await scoreAddOrUpdatePipeline.ExecuteAsync(rankedScore.Score, token);
        foreach (var context in result.ImpactedContextsWithPoints)
        {
            await memberPointStatsPipeline.ExecuteAsync(rankedScore.PlayerId, context);
            foreach (var pointId in context.Points.Select(x => x.Id))
                await memberLevelStatsPipeline.ExecuteAsync(rankedScore.PlayerId, context.GuildId, context.Id, pointId);
        }

        logger.LogInformation(
            "Processed confirmation side effects for ranked score {RankedScoreId} in context {ContextId}.",
            rankedScoreId, contextId);
    }
}