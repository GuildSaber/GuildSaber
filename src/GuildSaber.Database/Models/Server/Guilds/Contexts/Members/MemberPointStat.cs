using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class MemberPointStat
{
    public int Id { get; init; }
    public GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }
    public Player.PlayerId PlayerId { get; init; }
    public Point.PointId PointId { get; init; }
    public Category.CategoryId? CategoryId { get; init; }

    public float Points { get; set; }
    public float Xp { get; set; }
    public int PassCount { get; set; }
}

public class MemberPointStatConfiguration : IEntityTypeConfiguration<MemberPointStat>
{
    public void Configure(EntityTypeBuilder<MemberPointStat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.PlayerId, x.PointId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => new { x.ContextId, x.PointId, x.CategoryId });

        builder.HasOne<ContextMember>()
            .WithMany(x => x.PointStats)
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne<Point>()
            .WithMany()
            .HasForeignKey(x => x.PointId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}