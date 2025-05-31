using GuildSaber.Database.Extensions;
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

    public readonly record struct PointId(int Value) : IEFStrongTypedId<PointId, int>
    {
        public static bool TryParse(string from, out PointId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new PointId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(PointId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
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