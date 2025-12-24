using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class DiffStarLevel : Level
{
    public required RankedMapRating.DifficultyStar MinStar { get; set; }
    public required uint RequiredPassCount { get; set; }
}

public class DiffStarLevelConfiguration : IEntityTypeConfiguration<DiffStarLevel>
{
    public void Configure(EntityTypeBuilder<DiffStarLevel> builder)
    {
        builder.HasBaseType<Level>();

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName(nameof(DiffStarLevel.RequiredPassCount));

        builder.Property(x => x.MinStar)
            .HasConversion<float>(from => from, to => new RankedMapRating.DifficultyStar(to))
            .HasColumnName(nameof(DiffStarLevel.MinStar));
    }
}