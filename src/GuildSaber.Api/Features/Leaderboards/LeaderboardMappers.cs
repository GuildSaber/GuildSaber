using System.Linq.Expressions;
using GuildSaber.Api.Features.Players;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Leaderboards;

public static class LeaderboardMappers
{
    public static Expression<Func<MemberStat, LeaderboardResponses.MemberStat>>
        MapMemberStatExpression(ServerDbContext dbContext)
        => self => new LeaderboardResponses.MemberStat
        {
            Points = self.Points,
            Xp = self.Xp,
            LevelId = self.LevelId,
            NextLevelId = self.NextLevelId,
            Player = dbContext.Players
                .Where(p => p.Id == self.PlayerId)
                .Select(PlayerMappers.MapPlayerExpression)
                .First()
        };
}