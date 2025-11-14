using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Members;

public class MemberStat
{
    public Guild.GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }
    public Player.PlayerId PlayerId { get; init; }
    public Point.PointId PointId { get; init; }

    public required float Points { get; set; }
    public required float Xp { get; set; }
    public required int PassCount { get; set; }

    public required Level.LevelId? LevelId { get; set; }
    public required Level.LevelId? NextLevelId { get; set; }

    public Level? Level { get; set; }
    public Level? NextLevel { get; set; }
}

public class MemberStatConfiguration : IEntityTypeConfiguration<MemberStat>
{
    public void Configure(EntityTypeBuilder<MemberStat> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.ContextId, x.PlayerId, x.PointId });
        builder.HasIndex(x => new { x.GuildId, x.ContextId, x.PointId });

        builder.HasOne<ContextMember>()
            .WithMany()
            .HasForeignKey(x => new { x.GuildId, x.ContextId, x.PlayerId });

        builder.HasOne<Point>()
            .WithMany()
            .HasForeignKey(x => x.PointId);

        builder.HasOne(x => x.Level)
            .WithMany()
            .HasForeignKey(x => x.LevelId);

        builder.HasOne(x => x.NextLevel)
            .WithMany()
            .HasForeignKey(x => x.NextLevelId);
    }
}