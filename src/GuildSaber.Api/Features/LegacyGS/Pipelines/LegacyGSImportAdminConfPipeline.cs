using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;
using OldGSState = GuildSaber.Common.Services.LegacyGuildSaber.Models.EState;

namespace GuildSaber.Api.Features.LegacyGS.Pipelines;

public class LegacyGSImportAdminConfPipeline(
    ServerDbContext dbContext,
    LegacyGuildSaberApi legacyGuildSaberApi,
    TimeProvider timeProvider,
    ILogger<LegacyGSImportAdminConfPipeline> logger)
{
    /// <returns>True if any confirmations were imported; otherwise, false.</returns>
    public async Task<bool> ExecuteAsync(GuildId guildId, PlayerId playerId, CancellationToken token)
    {
        logger.LogInformation(
            "Importing legacy GuildSaber admin confirmations for player {PlayerId} in guild {GuildId}",
            playerId, guildId);
        var (beatleaderId, scoreSaberId) = await dbContext.Players
            .Where(x => x.Id == playerId)
            .Select(x => new Tuple<BeatLeaderId?, ScoreSaberId?>(
                x.LinkedAccounts.BeatLeaderId(),
                x.LinkedAccounts.ScoreSaberId))
            .FirstAsync(token);

        var impactedContextPoints = new HashSet<(ContextId, Point.PointId)>();
        var impactedRankedMapIds = new HashSet<RankedMapId>();
        await foreach (var data in dbContext.PendingRankedScores
                           .Where(x => x.GuildId == guildId && x.PlayerId == playerId)
                           .Select(x => new
                           {
                               x.Id,
                               x.ScoreId,
                               x.ContextId,
                               x.PointId,
                               x.RankedMapId,
                               x.Score.BaseScore,
                               x.SongDifficulty.BLLeaderboardId,
                               x.SongDifficulty.SSLeaderboardId
                           })
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
        {
            Result<OldGSState> result;
            try
            {
                result = await legacyGuildSaberApi.GetRankedScoreStateAsync(
                    guildId,
                    beatleaderId!.Value,
                    scoreSaberId,
                    blId: data.BLLeaderboardId,
                    ssId: data.SSLeaderboardId,
                    unmodifiedScore: data.BaseScore);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Error retrieving ranked score state from LegacyGuildSaber for ranked score {RankedScoreId} (ContextId: {ContextId}, PointId: {PointId})",
                    data.Id, data.ContextId, data.PointId);
                continue;
            }

            if (!result.TryGetValue(out var state) || state.HasFlag(OldGSState.NeedConfirmation))
                continue;

            var stateToSet = state switch
            {
                _ when state.HasAnyFlag(OldGSState.ScoringTeamConfirmed | OldGSState.Allowed) => RankedScore.ERankedScoreType.Accepted,
                _ when state.HasAnyFlag(OldGSState.ScoringTeamDenied | OldGSState.Denied) => RankedScore.ERankedScoreType.Refused,
                _ => (RankedScore.ERankedScoreType?)null
            };

            if (stateToSet is null)
                continue;

            var updatedCount = await RankedScoreService.UpdateRankedScoreConfirmationStateAsync(
                dbContext, timeProvider, data.ContextId, data.ScoreId, stateToSet.Value, token);
            if (updatedCount == 0)
                continue;

            var affectedRankedScores = await dbContext.RankedScores
                .Where(x => x.ContextId == data.ContextId
                            && x.ScoreId == data.ScoreId
                            && (x is PendingRankedScore || x is AcceptedRankedScore || x is RefusedRankedScore))
                .Select(x => new { x.ContextId, x.PointId, x.RankedMapId })
                .ToArrayAsync(token);

            foreach (var rankedScore in affectedRankedScores)
            {
                impactedContextPoints.Add((rankedScore.ContextId, rankedScore.PointId));
                impactedRankedMapIds.Add(rankedScore.RankedMapId);
            }
        }

        if (impactedContextPoints.Count == 0)
        {
            logger.LogInformation(
                "No legacy GuildSaber admin confirmations to import for player {PlayerId} in guild {GuildId}",
                playerId, guildId);
            return false;
        }

        logger.LogInformation("Completed importing {count} legacy GuildSaber admin confirmations for player {PlayerId}",
            impactedContextPoints.Count, playerId);
        await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync(impactedRankedMapIds, dbContext);

        return true;
    }
}