using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Server.Guilds.Guild.GuildId;

namespace GuildSaber.Database.Models.Server.Guilds.Points;

public class Point
{
    public PointId Id { get; init; }
    public GuildId GuildId { get; init; }

    public PointInfo Info { get; set; }
    public ModifierSettings ModifierSettings { get; set; }
    public CurveSettings CurveSettings { get; set; }
    public WeightingSettings WeightingSettings { get; set; }

    public readonly record struct PointId(int Value) : IEFStrongTypedId<PointId, int>;
}

public class PointConfiguration : IEntityTypeConfiguration<Point>
{
    public void Configure(EntityTypeBuilder<Point> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<Point.PointId, int>();
        builder.HasOne<Guild>().WithMany(x => x.Points).HasForeignKey(x => x.GuildId);
        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.ModifierSettings);
        builder.ComplexProperty(x => x.CurveSettings);
        builder.ComplexProperty(x => x.WeightingSettings);
    }
}