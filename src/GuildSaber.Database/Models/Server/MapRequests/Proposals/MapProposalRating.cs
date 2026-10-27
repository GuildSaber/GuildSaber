using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals;

public record MapProposalRating
{
    /// <summary>When omitted in a draft, calculated on submission.</summary>
    public RankedMapRating.AccuracyStar? AccStar { get; set; }

    /// <summary>When omitted in a draft, calculated on submission.</summary>
    public RankedMapRating.DifficultyStar? DiffStar { get; set; }

    /// <summary>Set on submission when the accuracy rating was calculated rather than supplied.</summary>
    public bool IsAccStarAutoCalculated { get; init; }

    /// <summary>Set on submission when the difficulty rating was calculated rather than supplied.</summary>
    public bool IsDiffStarAutoCalculated { get; init; }
}

public class MapProposalRatingConfiguration : IComplexPropertyConfiguration<MapProposalRating>
{
    public ComplexPropertyBuilder<MapProposalRating> Configure(ComplexPropertyBuilder<MapProposalRating> builder)
    {
        builder.Property(x => x.AccStar).HasConversion<float?>(
            from => from.HasValue ? from.Value.Value : null,
            to => to.HasValue ? new RankedMapRating.AccuracyStar(to.Value) : null);
        builder.Property(x => x.DiffStar).HasConversion<float?>(
            from => from.HasValue ? from.Value.Value : null,
            to => to.HasValue ? new RankedMapRating.DifficultyStar(to.Value) : null);

        return builder;
    }
}