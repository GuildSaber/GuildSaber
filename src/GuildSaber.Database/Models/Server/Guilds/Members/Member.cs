using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class Member
{
    public Guild.GuildId GuildId { get; init; }
    public Player.PlayerId PlayerId { get; init; }

    public required DateTimeOffset InitializedAt { get; init; }
    public required DateTimeOffset EditedAt { get; set; }
    public required EPermission Permissions { get; set; }
    public required EJoinState JoinState { get; set; }

    public Player Player { get; init; } = null!;
    public Guild Guild { get; init; } = null!;

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

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.PlayerId });
        builder.HasOne(x => x.Guild)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.GuildId);

        builder.HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId);
    }
}