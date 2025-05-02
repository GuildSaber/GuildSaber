using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.Players;
using GuildSaber.Database.Models.RankedMaps;
using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.RankedScores;

public class RankedScore
{
    public RankedScoreId Id { get; init; }

    public Guild.GuildId GuildId { get; set; }
    public RankedMap.RankedMapId RankedMapId { get; set; }
    public SongDifficulty.SongDifficultyId SongDifficultyId { get; set; }
    public Player.PlayerId PlayerId { get; set; }

    // public Score.ScoreId ScoreId { get; set; }
    // public Point.PointId PointId { get; set; }
    public readonly record struct RankedScoreId(uint Value) : IStrongType<uint>;
}

public class RankedScoreConfiguration : IEntityTypeConfiguration<RankedScore>
{
    public void Configure(EntityTypeBuilder<RankedScore> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedScore.RankedScoreId, uint>();
        builder.HasOne<Guild>().WithMany().HasForeignKey(x => x.GuildId);
        builder.HasOne<RankedMap>().WithMany().HasForeignKey(x => x.RankedMapId);
        builder.HasOne<SongDifficulty>().WithMany().HasForeignKey(x => x.SongDifficultyId);
        builder.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId);
    }
}