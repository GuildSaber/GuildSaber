using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

/// <remarks>
/// Class created to add future functionality such as join requirements (e.g. rank, invite code, etc.)
/// </remarks>
public class ContextMember
{
    public GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }
    public Player.PlayerId PlayerId { get; init; }

    public IList<MemberPointStat> PointStats { get; init; } = null!;
    public IList<MemberLevelStat> LevelStats { get; init; } = null!;
}

public class ContextMemberConfiguration : IEntityTypeConfiguration<ContextMember>
{
    public void Configure(EntityTypeBuilder<ContextMember> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne<Guild>()
            .WithMany()
            .HasForeignKey(x => x.GuildId);

        builder.HasOne<Context>()
            .WithMany(x => x.ContextMembers);
        builder.HasOne<Member>()
            .WithMany(x => x.ContextMembers);

        builder.HasMany(x => x.PointStats)
            .WithOne()
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasMany(x => x.LevelStats)
            .WithOne()
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });
    }
}