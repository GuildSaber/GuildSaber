using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Guilds.Members.AchievementStats.Http;

public static class AchievementStatMappers
{
    public static Expression<Func<MemberAchievementStat, AchievementStatResponses.MemberAchievementStat>>
        MapMemberAchievementStatExpression
        => self => new AchievementStatResponses.MemberAchievementStat(
            AchievementMappers.MapAchievementExpression.Invoke(self.Achievement),
            self.IsCompleted,
            self.IsLocked,
            self.PassCount
        );
}