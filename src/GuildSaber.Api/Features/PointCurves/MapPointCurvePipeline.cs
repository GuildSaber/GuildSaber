using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.PointCurves;

/// <summary>Reuses normal scoring transforms for one map/point, in bounded player pages.</summary>
public sealed class MapPointCurvePipeline(
    ServerDbContext db,
    ScoreAddOrUpdatePipeline scoreAddOrUpdatePipeline,
    PointCurveService curves,
    MemberStatsPipeline memberStatsPipeline,
    MemberAchievementStatsPipeline achievements)
{
    private const int PlayerBatchSize = 128;

    public async Task ExecuteAsync(
        RankedMapId mapId, Point.PointId pointId, bool regenerate,
        PlayerId? affectedPlayer = null, bool refreshMemberStats = true, CancellationToken token = default)
    {
        var context = await db.Contexts
            .Where(x =>
                x.RankedMaps.Any(m => m.Id == mapId)
                && x.Points.Any(p => p.Id == pointId))
            .Include(x => x.Points.Where(p => p.Id == pointId))
            .SingleOrDefaultAsync(token);

        if (context is null)
            return;

        var point = context.Points.Single(x => x.Id == pointId);
        context.Points = [point];

        // Bootstrap the new point's own eligibility before deriving benchmarks. No approval is inherited.
        await ScorePlayersAsync();

        if (regenerate && await curves.RefreshAsync(mapId, pointId, token))
            await ScorePlayersAsync();

        await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync([mapId], db, pointId, token);

        if (refreshMemberStats
            && affectedPlayer is { } playerId
            && await db.ContextMembers.AnyAsync(x => x.ContextId == context.Id && x.PlayerId == playerId, token))
        {
            // Cascading deletion may have removed the previous selected pass before recalculation could compare it.
            await achievements.ExecuteAsync(playerId, context.GuildId, context.Id, pointId, token);
            db.ChangeTracker.Clear();
        }

        if (refreshMemberStats)
            await memberStatsPipeline.ExecuteAsync(context, Players(), refreshAchievements: false, token);

        return;

        IQueryable<PlayerId> Players()
            => db.Scores
                .Where(x =>
                    db.MapVersions.Any(v =>
                        v.RankedMapId == mapId
                        && v.SongDifficultyId == x.SongDifficultyId)
                    && db.ContextMembers.Any(m =>
                        m.ContextId == context.Id
                        && m.PlayerId == x.PlayerId))
                .Select(x => x.PlayerId)
                .Concat(db.RankedScores
                    .Where(x =>
                        x.RankedMapId == mapId
                        && x.PointId == pointId
                        && db.ContextMembers.Any(m =>
                            m.ContextId == context.Id
                            && m.PlayerId == x.PlayerId))
                    .Select(x => x.PlayerId))
                .Concat(db.ContextMembers
                    .Where(x =>
                        x.ContextId == context.Id
                        && x.PlayerId == affectedPlayer)
                    .Select(x => x.PlayerId))
                .Distinct();

        async Task ScorePlayersAsync()
        {
            var after = 0;

            while (true)
            {
                var boundary = new PlayerId(after);

                var players = await Players()
                    .Where(x => x > boundary)
                    .OrderBy(x => x)
                    .Take(PlayerBatchSize)
                    .ToArrayAsync(token);

                if (players.Length == 0)
                    break;

                foreach (var player in players)
                {
                    var passChanged = await scoreAddOrUpdatePipeline.RecalculatePlayerScoresForMapAsync(
                        mapId, pointId, player, updateRanks: false, token);

                    if (!refreshMemberStats || !passChanged || player == affectedPlayer)
                        continue;

                    // Point-wide settings jobs refresh achievements once in their final member pass.
                    await achievements.ExecuteAsync(player, context.GuildId, context.Id, pointId, token);
                    db.ChangeTracker.Clear();
                }

                after = players[^1].Value;
            }
        }
    }
}
