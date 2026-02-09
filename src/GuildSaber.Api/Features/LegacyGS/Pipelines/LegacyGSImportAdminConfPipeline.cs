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
            .Select(x => new Tuple<BeatLeaderId, ScoreSaberId?>(
                x.LinkedAccounts.BeatLeaderId,
                x.LinkedAccounts.ScoreSaberId))
            .FirstAsync(token);

        var impactedContextPoints = new HashSet<(ContextId, Point.PointId)>();
        await foreach (var data in dbContext.RankedScores
                           .Where(x => x.PlayerId == playerId && x.State.HasFlag(RankedScore.EState.Pending))
                           .Select(x => new
                           {
                               x.Id,
                               x.ContextId,
                               x.PointId,
                               x.Score.BaseScore,
                               x.SongDifficulty.BLLeaderboardId,
                               x.SongDifficulty.SSLeaderboardId
                           })
                           .AsAsyncEnumerable()
                           .WithCancellation(token))
        {
            var result = await legacyGuildSaberApi.GetRankedScoreStateAsync(
                guildId,
                beatleaderId,
                scoreSaberId,
                blId: data.BLLeaderboardId,
                ssId: data.SSLeaderboardId,
                unmodifiedScore: data.BaseScore);

            if (!result.TryGetValue(out var state) || state.HasFlag(OldGSState.NeedConfirmation))
                continue;

            if (state.HasAnyFlag(OldGSState.ScoringTeamConfirmed | OldGSState.Allowed))
                await dbContext.RankedScores
                    .Where(x => x.Id == data.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.State,
                            y => y.State & ~RankedScore.EState.Pending | RankedScore.EState.Confirmed),
                        cancellationToken: token
                    );
            else if (state.HasAnyFlag(OldGSState.ScoringTeamDenied | OldGSState.Denied))
                await dbContext.RankedScores
                    .Where(x => x.Id == data.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(y => y.State,
                            y => y.State & ~RankedScore.EState.Pending | RankedScore.EState.Refused),
                        cancellationToken: token
                    );
            else continue;

            impactedContextPoints.Add((data.ContextId, data.PointId));
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

        return true;
    }
}