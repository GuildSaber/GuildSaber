using System.Runtime.CompilerServices;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Common.Helpers;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
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
    private static readonly string _updateRankedScoreTypeFormattableString =
        $$"""
          UPDATE "{{nameof(ServerDbContext.RankedScores)}}"
          SET "{{nameof(RankedScore.Type)}}" = {0},
              "{{nameof(PointGivingRankedScore.Rank)}}" = CASE WHEN {0} = {{(int)RankedScore.ERankedScoreType.Accepted}} THEN 0 ELSE NULL END,
              "{{nameof(RankedScore.EditedAt)}}" = {1}
          WHERE "{{nameof(RankedScore.Id)}}" = {2}
          """;

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
        var impactedRankedMapIds = new HashSet<RankedMap.RankedMapId>();
        await foreach (var data in dbContext.PendingRankedScores
                           .Where(x => x.PlayerId == playerId)
                           .Select(x => new
                           {
                               x.Id,
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

            if (state.HasAnyFlag(OldGSState.ScoringTeamConfirmed | OldGSState.Allowed))
                await UpdateRankedScoreTypeAsync(data.Id, RankedScore.ERankedScoreType.Accepted, token);
            else if (state.HasAnyFlag(OldGSState.ScoringTeamDenied | OldGSState.Denied))
                await UpdateRankedScoreTypeAsync(data.Id, RankedScore.ERankedScoreType.Refused, token);
            else continue;

            impactedContextPoints.Add((data.ContextId, data.PointId));
            impactedRankedMapIds.Add(data.RankedMapId);
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

    private Task<int> UpdateRankedScoreTypeAsync(
        RankedScoreId rankedScoreId, RankedScore.ERankedScoreType type, CancellationToken token) => dbContext.Database
        .ExecuteSqlAsync(
            FormattableStringFactory.Create(
                _updateRankedScoreTypeFormattableString,
                (int)type,
                timeProvider.GetUtcNow(),
                rankedScoreId.Value),
            token);
}