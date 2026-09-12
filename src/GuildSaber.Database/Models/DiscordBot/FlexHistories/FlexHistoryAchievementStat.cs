using GuildSaber.Database.Models.Server.Guilds.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.DiscordBot.FlexHistories;

/// <summary>
/// Stores the achievement per category for a flex history snapshot.
/// </summary>
public class FlexHistoryAchievementStat
{
    public FlexHistory.FlexHistoryId FlexHistoryId { get; init; }
    public CategoryId CategoryId { get; init; }
    public Achievement.AchievementId? AchievementId { get; set; }
}

public class FlexHistoryAchievementStatConfiguration : IEntityTypeConfiguration<FlexHistoryAchievementStat>
{
    public void Configure(EntityTypeBuilder<FlexHistoryAchievementStat> builder)
    {
        builder.HasKey(x => new { x.FlexHistoryId, x.CategoryId });

        builder.Property(x => x.FlexHistoryId)
            .HasConversion(id => id.Value, value => new FlexHistory.FlexHistoryId(value));
        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value));
        builder.Property(x => x.AchievementId)
            .HasConversion(id => id.HasValue ? id.Value.Value : (int?)null,
                value => value.HasValue ? new Achievement.AchievementId(value.Value) : null);
    }
}