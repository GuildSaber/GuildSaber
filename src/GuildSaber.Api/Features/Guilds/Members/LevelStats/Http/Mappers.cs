using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Levels.Http;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Guilds.Members.LevelStats.Http;

public static class LevelStatMappers
{
    public static Expression<Func<MemberLevelStat, LevelStatResponses.MemberLevelStat>>
        MapMemberLevelStatExpression
        => self => new LevelStatResponses.MemberLevelStat(
            LevelMappers.MapLevelExpression.Invoke(self.Level),
            self.IsCompleted,
            self.IsLocked,
            self.PassCount
        );
}