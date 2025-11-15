using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.RankedScores;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberStatPipeline(ServerDbContext dbContext)
{
    public async Task ExecuteAsync(PlayerId playerId, Context context)
    {
        ArgumentNullException.ThrowIfNull(context.Points);

        if (context.GuildId != 1) return;
        foreach (var point in context.Points)
        {
            var validPassesQuery = dbContext.RankedScores
                .Where(x =>
                    x.GuildId == context.GuildId &&
                    x.ContextId == context.Id &&
                    x.PlayerId == playerId &&
                    x.PointId == point.Id &&
                    x.State.HasFlag(RankedScore.EState.Selected) &&
                    ((int)x.State & (int)RankedScore.EState.NonPointGiving) == 0);

            var memberStat = await dbContext.MemberStats
                .FindAsync(context.GuildId, context.Id, playerId, point.Id);

            if (memberStat is null)
            {
                memberStat = new MemberStat
                {
                    GuildId = context.GuildId,
                    ContextId = context.Id,
                    PlayerId = playerId,
                    PointId = point.Id,
                    PassCount = validPassesQuery.Count(),
                    Points = point.WeightingSettings.IsEnabled
                        ? validPassesQuery.Sum(rs => (float)(rs.RawPoints * point.WeightingSettings.Multiplier))
                        : validPassesQuery.Sum(rs => rs.RawPoints),
                    Xp = 0,
                    LevelId = null,
                    NextLevelId = null
                };

                dbContext.MemberStats.Add(memberStat);
            }
            else
            {
                memberStat.Points = point.WeightingSettings.IsEnabled
                    ? validPassesQuery.Sum(rs => (float)(rs.RawPoints * point.WeightingSettings.Multiplier))
                    : validPassesQuery.Sum(rs => rs.RawPoints);
                memberStat.PassCount = validPassesQuery.Count();
            }
        }

        await dbContext.SaveChangesAsync();
    }
}