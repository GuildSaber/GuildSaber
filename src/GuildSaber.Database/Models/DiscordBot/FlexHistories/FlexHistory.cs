using GuildSaber.Database.Models.Server.Guilds.Levels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.DiscordBot.FlexHistories;

/// <summary>
/// Represents a snapshot of a player's flex history for a specific guild context at a point in time.
/// </summary>
public class FlexHistory
{
    public FlexHistoryId Id { get; init; }
    public required PlayerId PlayerId { get; init; }
    public required GuildId GuildId { get; init; }
    public required ContextId ContextId { get; init; }

    public required DateTimeOffset Timestamp { get; set; }

    public Level.LevelId? GlobalLevelId { get; set; }
    public IList<FlexHistoryLevelStat> LevelStats { get; set; } = [];
    public IList<FlexHistoryPointStat> PointStats { get; set; } = [];

    public readonly record struct FlexHistoryId(long Value)
    {
        public static implicit operator long(FlexHistoryId id) => id.Value;
        public override string ToString() => Value.ToString();
    }
}

public class FlexHistoryConfiguration : IEntityTypeConfiguration<FlexHistory>
{
    public void Configure(EntityTypeBuilder<FlexHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FlexHistory.FlexHistoryId(value))
            .ValueGeneratedOnAdd();

        builder.Property(x => x.PlayerId).HasConversion(id => id.Value, value => new PlayerId(value));
        builder.Property(x => x.GuildId).HasConversion(id => id.Value, value => new GuildId(value));
        builder.Property(x => x.ContextId).HasConversion(id => id.Value, value => new ContextId(value));
        builder.Property(x => x.GlobalLevelId).HasConversion(
            id => id.HasValue ? id.Value.Value : (int?)null,
            value => value.HasValue ? new Level.LevelId(value.Value) : null);

        builder.HasMany(x => x.LevelStats)
            .WithOne()
            .HasForeignKey(x => x.FlexHistoryId);

        builder.HasMany(x => x.PointStats)
            .WithOne()
            .HasForeignKey(x => x.FlexHistoryId);

        builder.HasIndex(x => new { x.PlayerId, x.GuildId, x.ContextId, x.Timestamp });
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.Timestamp });
    }
}