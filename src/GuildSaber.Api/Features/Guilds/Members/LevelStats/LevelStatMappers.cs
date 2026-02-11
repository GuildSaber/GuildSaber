using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Levels;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats;

public static class LevelStatMappers
{
    public static Expression<Func<MemberLevelStat, LevelStatResponses.MemberLevelStat>>
        MapMemberLevelStatExpression(ServerDbContext dbContext)
        => self => new LevelStatResponses.MemberLevelStat(
            dbContext.Levels
                .Where(l => l.Id == self.LevelId)
                .Select(LevelMappers.MapLevelExpression)
                .First(),
            self.IsCompleted,
            self.IsLocked,
            self.PassCount
        );
}