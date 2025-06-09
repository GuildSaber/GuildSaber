using System.Linq.Expressions;
using GuildSaber.Database.Models.Server.Guilds.Members;

namespace GuildSaber.Api.Features.Guilds.Members;

public static class MemberMappers
{
    public static Expression<Func<Member, MemberResponses.Member>> MapMemberExpression
        => self => new MemberResponses.Member
        {
            GuildId = self.GuildId,
            PlayerId = self.PlayerId,
            JoinState = self.JoinState.Map(),
            Permissions = self.Permissions.Map(),
            EditedAt = self.EditedAt,
            InitializedAt = self.CreatedAt
        };

    public static MemberResponses.Member Map(this Member self)
        => new()
        {
            GuildId = self.GuildId,
            PlayerId = self.PlayerId,
            JoinState = self.JoinState.Map(),
            Permissions = self.Permissions.Map(),
            EditedAt = self.EditedAt,
            InitializedAt = self.CreatedAt
        };

    public static MemberResponses.EJoinState Map(this Member.EJoinState self)
        => self switch
        {
            Member.EJoinState.None => MemberResponses.EJoinState.None,
            Member.EJoinState.Joined => MemberResponses.EJoinState.Joined,
            Member.EJoinState.Requested => MemberResponses.EJoinState.Requested,
            Member.EJoinState.Invited => MemberResponses.EJoinState.Invited,
            Member.EJoinState.Refused => MemberResponses.EJoinState.Refused,
            Member.EJoinState.Banned => MemberResponses.EJoinState.Banned,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };

    public static MemberResponses.EPermission Map(this Member.EPermission self)
        => self switch
        {
            Member.EPermission.None => MemberResponses.EPermission.None,
            Member.EPermission.GuildLeader => MemberResponses.EPermission.GuildLeader,
            Member.EPermission.RankingTeam => MemberResponses.EPermission.RankingTeam,
            Member.EPermission.ScoringTeam => MemberResponses.EPermission.ScoringTeam,
            Member.EPermission.MemberTeam => MemberResponses.EPermission.MemberTeam,
            _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
        };
}