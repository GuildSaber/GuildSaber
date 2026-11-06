using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberStatsPipeline(
    ServerDbContext dbContext,
    MemberPointStatsPipeline pointStatsPipeline,
    MemberAchievementStatsPipeline achievementStatsPipeline)
{
    private const int PlayerBatchSize = 128;
    private const int ContextBatchSize = 128;

    public async Task ExecuteAsync(
        Context context, IQueryable<PlayerId> candidates, bool refreshAchievements = true,
        CancellationToken token = default)
    {
        var cursor = 0;

        while (true)
        {
            var boundary = new PlayerId(cursor);

            var players = await candidates
                .Where(x => x > boundary)
                .OrderBy(x => x)
                .Take(PlayerBatchSize)
                .ToArrayAsync(token);

            if (players.Length == 0)
                break;

            foreach (var player in players)
            {
                await pointStatsPipeline.ExecuteAsync(player, context, token);

                if (refreshAchievements)
                    foreach (var point in context.Points)
                        await achievementStatsPipeline.ExecuteAsync(
                            player, context.GuildId, context.Id, point.Id, token);

                dbContext.ChangeTracker.Clear();
            }

            cursor = players[^1].Value;
        }
    }

    public async Task RecalculateForPointAsync(Point.PointId pointId, CancellationToken token)
    {
        var cursor = 0;

        while (true)
        {
            var boundary = new ContextId(cursor);

            var contexts = await dbContext.Contexts
                .Where(x =>
                    x.Id > boundary
                    && x.Points.Any(p => p.Id == pointId))
                .Include(x => x.Points.Where(p => p.Id == pointId))
                .OrderBy(x => x.Id)
                .Take(ContextBatchSize)
                .ToArrayAsync(token);

            if (contexts.Length == 0)
                break;

            foreach (var context in contexts)
            {
                var players = dbContext.ContextMembers
                    .Where(x =>
                        x.ContextId == context.Id
                        && (dbContext.RankedScores.Any(s =>
                                s.ContextId == context.Id
                                && s.PointId == pointId
                                && s.PlayerId == x.PlayerId)
                            || dbContext.MemberPointStats.Any(s =>
                                s.ContextId == context.Id
                                && s.PointId == pointId
                                && s.PlayerId == x.PlayerId)))
                    .Select(x => x.PlayerId);

                await ExecuteAsync(context, players, token: token);
            }

            cursor = contexts[^1].Id.Value;
        }
    }
}
