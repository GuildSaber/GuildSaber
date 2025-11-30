using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public class RankedMap
{
    public RankedMapId Id { get; init; }

    public Guild.GuildId GuildId { get; init; }
    public Context.ContextId ContextId { get; init; }

    public required RankedMapInfo Info { get; set; }
    public required RankedMapRequirements Requirements { get; init; }
    public required RankedMapRating Rating { get; init; }

    public IList<MapVersion> MapVersions { get; init; } = null!;
    public IList<Category> Categories { get; init; } = null!;
    public IList<RankedMapListLevel> Levels { get; init; } = null!;

    public readonly record struct RankedMapId(long Value) : IEFStrongTypedId<RankedMapId, long>
    {
        public static bool TryParse(string from, out RankedMapId value)
        {
            if (long.TryParse(from, out var id))
            {
                value = new RankedMapId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator long(RankedMapId id)
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
        builder.Property(x => x.Id).HasGenericConversion<RankedMap.RankedMapId, long>()
            .ValueGeneratedOnAdd();
        builder.HasIndex(x => x.ContextId);

        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.Requirements).Configure(new RankedMapRequirementsConfiguration());
        builder.ComplexProperty(x => x.Rating).Configure(new RankedMapRatingConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.RankedMaps).HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Context>()
            .WithMany(x => x.RankedMaps).HasForeignKey(x => x.ContextId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Categories).WithMany();
        builder.HasMany(x => x.Levels).WithMany(x => x.RankedMaps);
    }
}