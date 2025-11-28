using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class MemberLevelStat
{
    public int Id { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }
    public Player.PlayerId PlayerId { get; init; }
    public Level.LevelId LevelId { get; init; }
    public Category.CategoryId? CategoryId { get; init; }

    public bool IsCompleted { get; set; }
    public bool IsLocked { get; set; }

    // Xp won't be used until the feature is implemented.
    //public int? CurrentXp { get; set; }
    public int PassCount { get; set; }

    public Level Level { get; set; } = null!;
}

public class MemberLevelStatConfiguration : IEntityTypeConfiguration<MemberLevelStat>
{
    public void Configure(EntityTypeBuilder<MemberLevelStat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.PlayerId, x.LevelId, x.CategoryId }).IsUnique();
        builder.HasIndex(x => new { x.ContextId, x.PlayerId, x.CategoryId });
        builder.HasIndex(x => new { x.ContextId, x.PlayerId });

        builder.HasOne<ContextMember>()
            .WithMany(x => x.LevelStats)
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne(x => x.Level)
            .WithMany()
            .HasForeignKey(x => x.LevelId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}