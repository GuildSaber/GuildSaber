using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace GuildSaber.Database.Models.Server.Guilds.Points;

public record CurveSettings
{
    public required CustomCurve Difficulty { get; set; }
    public required CustomCurve Accuracy { get; set; }
}

public class CurveSettingsConfiguration : IComplexPropertyConfiguration<CurveSettings>
{
    public ComplexPropertyBuilder<CurveSettings> Configure(ComplexPropertyBuilder<CurveSettings> builder)
    {
        builder.Property(x => x.Difficulty)
            .HasColumnType("point[]")
            .HasConversion(
                from => Array.ConvertAll(from.Points, p => new NpgsqlPoint(p.X, p.Y)),
                to => new CustomCurve(Array.ConvertAll(to, p => new CurvePoint(p.X, p.Y))));

        builder.Property(x => x.Accuracy)
            .HasColumnType("point[]")
            .HasConversion(
                from => Array.ConvertAll(from.Points, p => new NpgsqlPoint(p.X, p.Y)),
                to => new CustomCurve(Array.ConvertAll(to, p => new CurvePoint(p.X, p.Y))));

        return builder;
    }
}

public readonly record struct CurvePoint(double X, double Y)
{
    /// <remarks>
    /// Makes initialization with (x, y) tuple syntax easier.
    /// </remarks>
    public static implicit operator CurvePoint((double X, double Y) tuple)
        => new(tuple.X, tuple.Y);
}

public record CustomCurve(CurvePoint[] Points)
{
    public double ProjectValue(double value)
    {
        double result = 0;
        var index = 0;

        while (index < Points.Length - 1)
        {
            if (value <= Points[index].X && value >= Points[index + 1].X)
            {
                result = Points[index].Y + (Points[index + 1].Y - Points[index].Y) *
                    (value - Points[index].X) / (Points[index + 1].X - Points[index].X);
                break;
            }

            index++;
        }

        return result;
    }
}