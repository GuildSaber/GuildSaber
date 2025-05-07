using GuildSaber.Database.Models.StrongTypes.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GuildId = GuildSaber.Database.Models.Guilds.Guild.GuildId;

namespace GuildSaber.Database.Models.Guilds.Points;

public class Point
{
    public PointId Id { get; init; }
    public GuildId GuildId { get; init; }
    
    public ModifierSettings ModifierSettings { get; set; }
    public CurveSettings CurveSettings { get; set; }
    public WeightingSettings WeightingSettings { get; set; }

    public readonly record struct PointId(int Value) : IStrongType<int>;
}

public class PointConfiguration : IEntityTypeConfiguration<Point>
{
    public void Configure(EntityTypeBuilder<Point> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<Point.PointId, int>();
        builder.HasOne<Guild>().WithMany().HasForeignKey(x => x.GuildId);
        builder.ComplexProperty(x => x.ModifierSettings);
        builder.ComplexProperty(x => x.CurveSettings);
        builder.ComplexProperty(x => x.WeightingSettings);
    }
} 