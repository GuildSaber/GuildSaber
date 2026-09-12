using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Achievements.Types;

public class RankedMapListAchievement(
    Achievement.AchievementId id,
    GuildId guildId,
    ContextId contextId,
    CategoryId? categoryId,
    AchievementInfo info,
    AchievementDiscordBindings discordBindings,
    uint? progressionOrder,
    bool isLocking,
    Xp unlockXp,
    uint requiredPassCount
) : Achievement(id, guildId, contextId, categoryId, info, discordBindings, progressionOrder, isLocking, unlockXp)
{
    // Required by EFCore.
    private RankedMapListAchievement()
        : this(default, default, default, null, default, default, 0, false, default, 0) { }

    public uint RequiredPassCount { get; set; } = requiredPassCount;
    public IList<RankedMap> RankedMaps { get; set; } = [];
}

public class RankedMapListAchievementConfiguration : IEntityTypeConfiguration<RankedMapListAchievement>
{
    public void Configure(EntityTypeBuilder<RankedMapListAchievement> builder)
    {
        builder.HasBaseType<Achievement>();

        builder.HasMany(x => x.RankedMaps)
            .WithMany(x => x.Achievements);
    }
}