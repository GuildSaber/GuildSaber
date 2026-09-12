using GuildSaber.Database.Models.Server.Guilds.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class MemberAchievementStat
{
    public int Id { get; init; }
    public GuildId GuildId { get; init; }
    public ContextId ContextId { get; init; }
    public PlayerId PlayerId { get; init; }
    public Achievement.AchievementId AchievementId { get; init; }

    public bool IsCompleted { get; set; }
    public bool IsLocked { get; set; }

    // Xp won't be used until the feature is implemented.
    //public int? CurrentXp { get; set; }

    /// <summary>
    /// The number of ranked maps passed for a map-based achievement.
    /// </summary>
    /// <remarks>Nullable because XP-based achievements won't use it.</remarks>
    public int? PassCount { get; set; }

    public Achievement Achievement { get; set; } = null!;
}

public class MemberAchievementStatConfiguration : IEntityTypeConfiguration<MemberAchievementStat>
{
    public void Configure(EntityTypeBuilder<MemberAchievementStat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.PlayerId, x.AchievementId }).IsUnique();
        builder.HasIndex(x => new { x.ContextId, x.PlayerId });

        builder.HasOne<ContextMember>()
            .WithMany(x => x.AchievementStats)
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne(x => x.Achievement)
            .WithMany()
            .HasForeignKey(x => x.AchievementId);
    }
}