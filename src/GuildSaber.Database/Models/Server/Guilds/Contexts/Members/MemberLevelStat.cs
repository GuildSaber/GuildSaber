using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class MemberLevelStat
{
    public int Id { get; init; }
    public GuildId GuildId { get; init; }
    public ContextId ContextId { get; init; }
    public Player.PlayerId PlayerId { get; init; }
    public Level.LevelId LevelId { get; init; }

    public bool IsCompleted { get; set; }
    public bool IsLocked { get; set; }

    // Xp won't be used until the feature is implemented.
    //public int? CurrentXp { get; set; }

    /// <summary>
    /// The number of ranked maps passed for the RankedMapList level type.
    /// </summary>
    /// <remarks>Nullable because star and xp-based levels won't use it.</remarks>
    public int? PassCount { get; set; }

    public Level Level { get; set; } = null!;
}

public class MemberLevelStatConfiguration : IEntityTypeConfiguration<MemberLevelStat>
{
    public void Configure(EntityTypeBuilder<MemberLevelStat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.PlayerId, x.LevelId }).IsUnique();
        builder.HasIndex(x => new { x.ContextId, x.PlayerId });

        builder.HasOne<ContextMember>()
            .WithMany(x => x.LevelStats)
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne(x => x.Level)
            .WithMany()
            .HasForeignKey(x => x.LevelId);
    }
}