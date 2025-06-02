using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public class RankedMap
{
    public RankedMapId Id { get; init; }

    public Guild.GuildId GuildId { get; init; }
    public GuildContext.GuildContextId ContextId { get; init; }
    public Song.SongId SongId { get; init; }
    public SongDifficulty.SongDifficultyId SongDifficultyId { get; init; }

    public RankedMapRequirements Requirements { get; init; }
    public IList<MapVersion> MapVersions { get; init; } = null!;

    public readonly record struct RankedMapId(ulong Value) : IEFStrongTypedId<RankedMapId, ulong>
    {
        public static bool TryParse(string from, out RankedMapId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new RankedMapId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(RankedMapId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class RankedMapConfiguration : IEntityTypeConfiguration<RankedMap>
{
    public void Configure(EntityTypeBuilder<RankedMap> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<RankedMap.RankedMapId, ulong>();

        builder.ComplexProperty(x => x.Requirements);

        builder.HasOne<Guild>()
            .WithMany(x => x.RankedMaps).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<GuildContext>()
            .WithMany().HasForeignKey(x => x.ContextId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Song>()
            .WithMany().HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<SongDifficulty>()
            .WithMany().HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}