using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Songs;
using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.RankedMaps;

public class RankedMap
{
    public RankedMapId Id { get; init; }

    public Guild.GuildId GuildId { get; init; }
    public GuildContext.GuildContextId ContextId { get; init; }
    public Song.SongId SongId { get; init; }
    public SongDifficulty.SongDifficultyId SongDifficultyId { get; init; }

    public RankedMapRequirements Requirements { get; init; }
    public IList<MapVersion> MapVersions { get; init; } = null!;
    public readonly record struct RankedMapId(ulong Value) : IStrongType<ulong>;
}

public class RankedMapConfiguration : IEntityTypeConfiguration<RankedMap>
{
    public void Configure(EntityTypeBuilder<RankedMap> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedMap.RankedMapId, ulong>();
        builder.HasOne<Guild>().WithMany().HasForeignKey(x => x.GuildId);
        builder.HasOne<GuildContext>().WithMany().HasForeignKey(x => x.ContextId);
        builder.HasOne<Song>().WithMany().HasForeignKey(x => x.SongId);
        builder.HasOne<SongDifficulty>().WithMany().HasForeignKey(x => x.SongDifficultyId);
        builder.ComplexProperty(x => x.Requirements);
    }
}