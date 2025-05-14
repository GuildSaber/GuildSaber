using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Server.Guilds.Guild.GuildId;
using PlayerId = GuildSaber.Database.Models.Server.Players.Player.PlayerId;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using ScoreId = GuildSaber.Database.Models.Server.Scores.AbstractScore.ScoreId;
using PointId = GuildSaber.Database.Models.Server.Guilds.Points.Point.PointId;

namespace GuildSaber.Database.Models.Server.RankedScores;

public class RankedScore
{
    public RankedScoreId Id { get; init; }

    public GuildId GuildId { get; init; }
    public GuildContext.GuildContextId ContextId { get; init; }
    public RankedMap.RankedMapId RankedMapId { get; init; }
    public SongDifficultyId SongDifficultyId { get; init; }
    public PointId PointId { get; init; }
    public PlayerId PlayerId { get; init; }

    public ScoreId ScoreId { get; set; }
    public ScoreId PrevScoreId { get; set; }

    public readonly record struct RankedScoreId(ulong Value) : IStrongType<ulong>;
}

public class RankedScoreConfiguration : IEntityTypeConfiguration<RankedScore>
{
    public void Configure(EntityTypeBuilder<RankedScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedScore.RankedScoreId, ulong>();

        builder.HasOne<Guild>()
            .WithMany().HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<GuildContext>()
            .WithMany().HasForeignKey(x => x.ContextId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RankedMap>()
            .WithMany().HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SongDifficulty>()
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Player>()
            .WithMany().HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AbstractScore>()
            .WithMany().HasForeignKey(x => x.ScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AbstractScore>()
            .WithMany().HasForeignKey(x => x.PrevScoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Point>()
            .WithMany().HasForeignKey(x => x.PointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}