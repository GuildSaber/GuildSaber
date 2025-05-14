using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using PlayModeId = GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes.PlayMode.PlayModeId;

namespace GuildSaber.Database.Models.Server.RankedMaps.MapVersions;

public class MapVersion
{
    private MapVersion(SongDifficultyId songDifficultyId, PlayModeId playModeId, DateTimeOffset addedAt, byte order)
        => (SongDifficultyId, PlayModeId, AddedAt, Order) = (songDifficultyId, playModeId, addedAt, order);

    private MapVersion() { }
    public SongDifficultyId SongDifficultyId { get; init; }
    public PlayModeId PlayModeId { get; init; }

    public DateTimeOffset AddedAt { get; init; }
    public byte Order { get; set; }

    public static Result<MapVersion> TryCreate(
        SongDifficultyId songDifficultyId, PlayModeId playModeId, DateTimeOffset addedAt, byte order)
        => addedAt == default
            ? Failure<MapVersion>("The provided date must not be a default date.")
            : Success(new MapVersion(songDifficultyId, playModeId, addedAt, order));
}

public class MapVersionConfiguration : IEntityTypeConfiguration<MapVersion>
{
    public void Configure(EntityTypeBuilder<MapVersion> builder)
    {
        builder.HasKey(x => new { x.SongDifficultyId, x.PlayModeId });
        builder.HasOne<SongDifficulty>().WithMany().HasForeignKey(x => x.SongDifficultyId);
        builder.HasOne<PlayMode>().WithMany().HasForeignKey(x => x.PlayModeId);
    }
}