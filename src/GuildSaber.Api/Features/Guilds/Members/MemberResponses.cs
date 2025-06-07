namespace GuildSaber.Api.Features.Guilds.Members;

public static class MemberResponses
{
    public readonly record struct Member(
        uint PlayerId,
        uint GuildId,
        DateTimeOffset InitializedAt,
        DateTimeOffset EditedAt,
        EPermission Permissions,
        EJoinState JoinState
    );

    public enum EJoinState : uint
    {
        None = 0,
        Joined = 1 << 0,
        Requested = 1 << 1,
        Invited = 1 << 2,
        Refused = 1 << 3,
        Banned = 1 << 4
    }

    [Flags]
    public enum EPermission : uint
    {
        None = 0,
        GuildLeader = 1 << 0,
        RankingTeam = 1 << 1,
        ScoringTeam = 1 << 2,
        MemberTeam = 1 << 3
    }
}