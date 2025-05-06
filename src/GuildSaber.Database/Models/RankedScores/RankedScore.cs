using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.Players;
using GuildSaber.Database.Models.RankedMaps;
using GuildSaber.Database.Models.Scores;
using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Guilds.Guild.GuildId;
using PlayerId = GuildSaber.Database.Models.Players.Player.PlayerId;
using SongDifficultyId = GuildSaber.Database.Models.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using ScoreId = GuildSaber.Database.Models.Scores.AbstractScore.ScoreId;

namespace GuildSaber.Database.Models.RankedScores;

public class RankedScore
{
    public RankedScoreId Id { get; init; }

    public GuildId GuildId { get; init; }
    public RankedMap.RankedMapId RankedMapId { get; init; }
    public SongDifficultyId SongDifficultyId { get; init; }
    public PlayerId PlayerId { get; init; }

    public ScoreId ScoreId { get; set; }

    public ScoreId PrevScoreId { get; set; }
    // public Point.PointId PointId { get; set; }

    public readonly record struct RankedScoreId(ulong Value) : IStrongType<ulong>;
}

public class RankedScoreConfiguration : IEntityTypeConfiguration<RankedScore>
{
    public void Configure(EntityTypeBuilder<RankedScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedScore.RankedScoreId, ulong>();
        builder.HasOne<Guild>().WithMany().HasForeignKey(x => x.GuildId);
        builder.HasOne<RankedMap>().WithMany().HasForeignKey(x => x.RankedMapId);
        builder.HasOne<SongDifficulty>().WithMany().HasForeignKey(x => x.SongDifficultyId);
        builder.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId);
        builder.HasOne<AbstractScore>().WithMany().HasForeignKey(x => x.ScoreId);
        builder.HasOne<AbstractScore>().WithMany().HasForeignKey(x => x.PrevScoreId);
        // builder.HasOne<Point>().WithMany().HasForeignKey(x => x.PointId);
    }
}