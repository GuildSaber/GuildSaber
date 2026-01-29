using GuildSaber.Api.Features.Players.Pipelines;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberJoinPipeline(
    PlayerScoresPipeline pipeline,
    ILogger<MemberJoinPipeline> logger)
{
    public async Task ExecuteAsync(PlayerId playerId, CancellationToken token)
    {
        logger.LogInformation("Starting member join pipeline for player {PlayerId}.", playerId);

        /* Since ALL player scores are stored by default on ScoreAddOrUpdate pipeline when they create
         * their account, we don't need to re-import them here. */
        await pipeline.RecalculatePlayerScoresAsync(playerId, token);

        /* We needed the previous recalculation to have the RankedScores used to check for confirmation.
         * If any confirmation is imported, we need to recalculate again to update the scores accordingly. */
        var importedAny = await pipeline.ImportLegacyGuildSaberAdminConfirmationAsync(playerId, token);
        if (importedAny) await pipeline.RecalculatePlayerScoresAsync(playerId, token);

        logger.LogInformation("Finished member join pipeline for player {PlayerId}.", playerId);
    }
}