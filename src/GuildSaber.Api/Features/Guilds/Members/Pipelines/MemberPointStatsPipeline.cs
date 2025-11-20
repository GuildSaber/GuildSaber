using System.Linq.Expressions;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Guilds.Members.Pipelines;

public sealed class MemberPointStatsPipeline(ServerDbContext dbContext)
{
    public static Expression<Func<RankedScore, bool>> ValidPasses =>
        x => x.State.HasFlag(RankedScore.EState.Selected)
             && ((int)x.State & (int)RankedScore.EState.NonPointGiving) == 0;

    public async Task ExecuteAsync(PlayerId playerId, Context context)
    {
        ArgumentNullException.ThrowIfNull(context.Points);

        //TODO: Remove this guild check when RankedScore pipeline is ready.
        if (context.GuildId != 1) return;

        var categories = await dbContext.Categories
            .Where(c => c.GuildId == context.GuildId)
            .ToListAsync();

        foreach (var point in context.Points)
        {
            await GetOrCreateMemberStatAsync(
                context.GuildId, context.Id, playerId, null, point);

            foreach (var category in categories)
                await GetOrCreateMemberStatAsync(
                    context.GuildId, context.Id, playerId, category.Id, point);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task GetOrCreateMemberStatAsync(
        GuildId guildId,
        ContextId contextId,
        PlayerId playerId,
        Category.CategoryId? categoryId,
        Point point)
    {
        var validPassesQuery = dbContext.RankedScores
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PlayerId == playerId &&
                x.PointId == point.Id)
            .Where(ValidPasses);

        if (categoryId is not null)
            validPassesQuery = validPassesQuery.Where(x => x.RankedMap.Categories.Any(c => c.Id == categoryId));

        var memberStat = await dbContext.MemberStats
            .Where(x =>
                x.GuildId == guildId &&
                x.ContextId == contextId &&
                x.PlayerId == playerId &&
                x.PointId == point.Id &&
                x.CategoryId == categoryId)
            .FirstOrDefaultAsync();

        if (memberStat is null)
        {
            memberStat = new MemberPointStat
            {
                GuildId = guildId,
                ContextId = contextId,
                PlayerId = playerId,
                PointId = point.Id,
                CategoryId = categoryId
            };

            dbContext.MemberStats.Add(memberStat);
        }

        //BUG: Weighting is not applied correctly based on the order of the score in the expo decrease.
        memberStat.Points = point.WeightingSettings.IsEnabled
            ? validPassesQuery.Sum(rs => (float)(rs.RawPoints * point.WeightingSettings.Multiplier))
            : validPassesQuery.Sum(rs => rs.RawPoints);
        memberStat.PassCount = validPassesQuery.Count();
    }
}