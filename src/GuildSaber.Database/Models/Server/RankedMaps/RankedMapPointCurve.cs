using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public class RankedMapPointCurve
{
    public RankedMapId RankedMapId { get; init; }
    public Point.PointId PointId { get; init; }

    public required CustomCurve Curve { get; set; }
    public double[] BenchmarkAccuracies { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RankedMapPointCurveConfiguration : IEntityTypeConfiguration<RankedMapPointCurve>
{
    public void Configure(EntityTypeBuilder<RankedMapPointCurve> builder)
    {
        builder.HasKey(x => new { x.RankedMapId, x.PointId });
        builder.HasIndex(nameof(RankedMapPointCurve.PointId), nameof(RankedMapPointCurve.RankedMapId));

        builder.Property(x => x.PointId)
            .HasConversion(x => x.Value, x => new Point.PointId(x));

        builder.Property(x => x.RankedMapId)
            .HasConversion(x => x.Value, x => new RankedMapId(x));

        builder.Property(x => x.Curve)
            .HasColumnType("point[]")
            .HasConversion(
                from => Array.ConvertAll(from.Points, p => new NpgsqlPoint(p.X, p.Y)),
                to => new CustomCurve(Array.ConvertAll(to, p => new CurvePoint(p.X, p.Y))));

        builder.HasOne<RankedMap>()
            .WithMany()
            .HasForeignKey(x => x.RankedMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Point>()
            .WithMany()
            .HasForeignKey(x => x.PointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}