using System.Globalization;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public record RankedMapRating
{
    public required AccuracyStar AccStar { get; set; }
    public required DifficultyStar DiffStar { get; set; }

    public readonly record struct AccuracyStar(float Value)
    {
        public static implicit operator float(AccuracyStar id)
            => id.Value;

        public override string ToString()
            => Value.ToString(CultureInfo.InvariantCulture);
    }

    public readonly record struct DifficultyStar(float Value)
    {
        public static implicit operator float(DifficultyStar id)
            => id.Value;

        public override string ToString()
            => Value.ToString(CultureInfo.InvariantCulture);
    }
}

public class RankedMapRatingConfiguration : IComplexPropertyConfiguration<RankedMapRating>
{
    public ComplexPropertyBuilder<RankedMapRating> Configure(ComplexPropertyBuilder<RankedMapRating> builder)
    {
        builder.Property(x => x.AccStar)
            .HasConversion(from => from.Value, to => new RankedMapRating.AccuracyStar(to));
        builder.Property(x => x.DiffStar)
            .HasConversion(from => from.Value, to => new RankedMapRating.DifficultyStar(to));

        return builder;
    }
}