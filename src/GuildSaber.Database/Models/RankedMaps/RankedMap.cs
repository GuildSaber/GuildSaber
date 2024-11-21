using GuildSaber.Database.Models.Guilds;
using GuildSaber.Database.Models.Songs;
using GuildSaber.Database.Models.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.RankedMaps;

public class RankedMap
{
    public readonly record struct RankedMapId(ulong Value) : IStrongType<ulong>;

    public RankedMapId Id { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public GuildContext.GuildContextId ContextId { get; init; }
    public Song.SongId SongId { get; init; }
    public SongDifficulty.SongDifficultyId SongDifficultyId { get; init; }
}

public class RankedMapConfiguration : IEntityTypeConfiguration<RankedMap>
{
    public void Configure(EntityTypeBuilder<RankedMap> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedMap.RankedMapId, ulong>();
    }
}