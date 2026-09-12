using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

/// <remarks>
/// Class created to add future functionality such as join requirements (e.g. rank, invite code, etc.)
/// </remarks>
public class ContextMember
{
    public GuildId GuildId { get; init; }
    public ContextId ContextId { get; init; }
    public PlayerId PlayerId { get; init; }

    public Context Context { get; init; } = null!;
    public Member Member { get; init; } = null!;
    public IList<MemberPointStat> PointStats { get; init; } = null!;
    public IList<MemberAchievementStat> AchievementStats { get; init; } = null!;
}

public class ContextMemberConfiguration : IEntityTypeConfiguration<ContextMember>
{
    public void Configure(EntityTypeBuilder<ContextMember> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne<Guild>()
            .WithMany()
            .HasForeignKey(x => x.GuildId);

        builder.HasOne(x => x.Context)
            .WithMany(x => x.ContextMembers);
        builder.HasOne(x => x.Member)
            .WithMany(x => x.ContextMembers);

        builder.HasMany(x => x.PointStats)
            .WithOne()
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasMany(x => x.AchievementStats)
            .WithOne()
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });
    }
}