using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Levels;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedScores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public class RankedMap
{
    public RankedMapId Id { get; init; }

    public GuildId GuildId { get; init; }
    public ContextId ContextId { get; init; }

    public required RankedMapInfo Info { get; set; }
    public required RankedMapRequirements Requirements { get; set; }
    public required RankedMapRating Rating { get; init; }

    public IList<MapVersion> MapVersions { get; init; } = null!;
    public IList<Category> Categories { get; init; } = null!;
    public IList<RankedMapListLevel> Levels { get; init; } = null!;
    public IList<RankedScore> RankedScores { get; init; } = null!;
}

public class RankedMapConfiguration : IEntityTypeConfiguration<RankedMap>
{
    public void Configure(EntityTypeBuilder<RankedMap> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new RankedMapId(x))
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

        builder.HasMany(x => x.MapVersions).WithOne()
            .HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RankedScores).WithOne()
            .HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}