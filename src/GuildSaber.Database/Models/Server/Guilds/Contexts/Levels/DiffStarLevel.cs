using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class DiffStarLevel : Level
{
    public required RankedMapRating.DifficultyStar? MinDiffStar { get; set; }
    public required uint RequiredPassCount { get; set; }
}

public class DiffStarLevelConfiguration : IEntityTypeConfiguration<DiffStarLevel>
{
    public void Configure(EntityTypeBuilder<DiffStarLevel> builder)
    {
        builder.HasBaseType<Level>();

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName(nameof(RankedMapListLevel.RequiredPassCount));

        builder.Property(x => x.MinDiffStar)
            .HasConversion<float?>(from => from.HasValue ? from.Value.Value : null,
                to => to.HasValue ? new RankedMapRating.DifficultyStar(to.Value) : null);
    }
}