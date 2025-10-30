using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RankedMapId = GuildSaber.Database.Models.Server.RankedMaps.RankedMap.RankedMapId;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using SongId = GuildSaber.Database.Models.Server.Songs.Song.SongId;
using PlayModeId = GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes.PlayMode.PlayModeId;

namespace GuildSaber.Database.Models.Server.RankedMaps.MapVersions;

public class MapVersion
{
    public RankedMapId RankedMapId { get; init; }
    public SongDifficultyId SongDifficultyId { get; init; }
    public PlayModeId PlayModeId { get; init; }

    /// <remarks>
    /// Used for efficient querying without needing to join the SongDifficulty table. (As an indexed column)
    /// Denormalized data, but a non issue since there are no semantically good reasons for a SongDifficulty to change
    /// its SongId.
    /// </remarks>
    public SongId SongId { get; init; }

    public DateTimeOffset AddedAt { get; init; }
    public byte Order { get; set; }

    public SongDifficulty SongDifficulty { get; init; } = null!;
    public Song Song { get; init; } = null!;
}

public class MapVersionConfiguration : IEntityTypeConfiguration<MapVersion>
{
    public void Configure(EntityTypeBuilder<MapVersion> builder)
    {
        builder.HasKey(x => new { x.RankedMapId, x.SongDifficultyId, x.PlayModeId });
        builder.HasOne<SongDifficulty>().WithMany().HasForeignKey(x => x.SongDifficultyId);
        builder.HasOne<PlayMode>().WithMany().HasForeignKey(x => x.PlayModeId);

        builder.HasOne(x => x.Song)
            .WithMany().HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SongDifficulty)
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}