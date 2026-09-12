using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Achievements.Types;

public class AccStarAchievement(
    Achievement.AchievementId id,
    GuildId guildId,
    ContextId contextId,
    CategoryId? categoryId,
    AchievementInfo info,
    AchievementDiscordBindings discordBindings,
    uint? progressionOrder,
    bool isLocking,
    Xp unlockXp,
    RankedMapRating.AccuracyStar minStar,
    uint requiredPassCount,
    RankedMapRating.AccuracyStar? maxStar = null
) : Achievement(id, guildId, contextId, categoryId, info, discordBindings, progressionOrder, isLocking, unlockXp)
{
    // Required by EFCore.
    private AccStarAchievement()
        : this(default, default, default, null, default, default, 0, false, default, default, 0) { }

    public RankedMapRating.AccuracyStar MinStar { get; set; } = minStar;
    public RankedMapRating.AccuracyStar? MaxStar { get; set; } = maxStar;
    public uint RequiredPassCount { get; set; } = requiredPassCount;
}

public class AccStarAchievementConfiguration : IEntityTypeConfiguration<AccStarAchievement>
{
    public void Configure(EntityTypeBuilder<AccStarAchievement> builder)
    {
        builder.HasBaseType<Achievement>();

        builder.Property(x => x.MinStar)
            .HasConversion<float>(from => from, to => new RankedMapRating.AccuracyStar(to))
            .HasColumnName("MinStar");

        builder.Property(x => x.MaxStar)
            .HasConversion<float?>(
                from => from.HasValue ? from.Value.Value : null,
                to => to.HasValue ? new RankedMapRating.AccuracyStar(to.Value) : null)
            .HasColumnName("MaxStar");

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName("RequiredPassCount");
    }
}