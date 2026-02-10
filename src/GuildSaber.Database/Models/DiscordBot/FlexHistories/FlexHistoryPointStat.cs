using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.DiscordBot.FlexHistories;

/// <summary>
/// Stores the point stats per point type for a flex history snapshot.
/// </summary>
public class FlexHistoryPointStat
{
    public FlexHistory.FlexHistoryId FlexHistoryId { get; init; }
    public Point.PointId PointId { get; init; }

    public int Rank { get; set; }
    public float Points { get; set; }
}

public class FlexHistoryPointStatConfiguration : IEntityTypeConfiguration<FlexHistoryPointStat>
{
    public void Configure(EntityTypeBuilder<FlexHistoryPointStat> builder)
    {
        builder.HasKey(x => new { x.FlexHistoryId, x.PointId });

        builder.Property(x => x.FlexHistoryId)
            .HasConversion(id => id.Value, value => new FlexHistory.FlexHistoryId(value));
        builder.Property(x => x.PointId)
            .HasConversion(id => id.Value, value => new Point.PointId(value));

        builder.HasIndex(x => new { x.FlexHistoryId, x.PointId }).IsUnique();
    }
}