using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class AccStarLevel : Level
{
    public required RankedMapRating.AccuracyStar MinStar { get; set; }
    public required uint RequiredPassCount { get; set; }
}

public class AccStarLevelConfiguration : IEntityTypeConfiguration<AccStarLevel>
{
    public void Configure(EntityTypeBuilder<AccStarLevel> builder)
    {
        builder.HasBaseType<Level>();

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName(nameof(AccStarLevel.RequiredPassCount));

        builder.Property(x => x.MinStar)
            .HasConversion<float>(from => from, to => new RankedMapRating.AccuracyStar(to))
            .HasColumnName(nameof(AccStarLevel.MinStar));
    }
}