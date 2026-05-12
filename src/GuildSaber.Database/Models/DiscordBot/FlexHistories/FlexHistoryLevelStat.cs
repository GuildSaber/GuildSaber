using GuildSaber.Database.Models.Server.Guilds.Levels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.DiscordBot.FlexHistories;

/// <summary>
/// Stores the level per category for a flex history snapshot.
/// </summary>
public class FlexHistoryLevelStat
{
    public FlexHistory.FlexHistoryId FlexHistoryId { get; init; }
    public CategoryId CategoryId { get; init; }
    public Level.LevelId? LevelId { get; set; }
}

public class FlexHistoryLevelStatConfiguration : IEntityTypeConfiguration<FlexHistoryLevelStat>
{
    public void Configure(EntityTypeBuilder<FlexHistoryLevelStat> builder)
    {
        builder.HasKey(x => new { x.FlexHistoryId, x.CategoryId });

        builder.Property(x => x.FlexHistoryId)
            .HasConversion(id => id.Value, value => new FlexHistory.FlexHistoryId(value));
        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value));
        builder.Property(x => x.LevelId)
            .HasConversion(id => id.HasValue ? id.Value.Value : (int?)null,
                value => value.HasValue ? new Level.LevelId(value.Value) : null);
    }
}