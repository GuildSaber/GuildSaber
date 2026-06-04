using System.Linq.Expressions;
using GuildSaber.Api.Features.Players.Http;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Leaderboards.Http;

public static class LeaderboardMappers
{
    public static Expression<Func<MemberPointStat, LeaderboardResponses.MemberPointStat>>
        MapMemberStatExpression => self => new LeaderboardResponses.MemberPointStat
    {
        Player = self.Player.Map(),
        Points = self.Points,
        PassCount = self.PassCount
    };
}