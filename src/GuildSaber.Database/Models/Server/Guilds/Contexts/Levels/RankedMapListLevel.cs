using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public class RankedMapListLevel : Level
{
    public IList<RankedMap> RankedMaps { get; set; } = null!;
    public required uint RequiredPassCount { get; set; }
}

public class RankedMapListLevelConfiguration : IEntityTypeConfiguration<RankedMapListLevel>
{
    public void Configure(EntityTypeBuilder<RankedMapListLevel> builder)
    {
        builder.HasBaseType<Level>();

        builder.Property(x => x.RequiredPassCount)
            .HasColumnName(nameof(RankedMapListLevel.RequiredPassCount));

        builder.HasMany(x => x.RankedMaps)
            .WithMany(x => x.Levels);
    }
}