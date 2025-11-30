using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class AccStarLevel : Level
{
    public required RankedMapRating.AccuracyStar MinAccStar { get; set; }
    public required uint RequiredPassCount { get; set; }
}

public class AccStarLevelConfiguration : IEntityTypeConfiguration<AccStarLevel>
{
    public void Configure(EntityTypeBuilder<AccStarLevel> builder)
    {
        builder.HasBaseType<Level>();

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName(nameof(RankedMapListLevel.RequiredPassCount));

        builder.Property(x => x.MinAccStar)
            .HasConversion<float>(from => from, to => new RankedMapRating.AccuracyStar(to));
    }
}