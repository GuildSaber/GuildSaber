using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public record RankedMapRequirements(
    bool NeedConfirmation,
    bool NeedFullCombo,
    float? MaxPauseDuration,
    AbstractScore.EModifiers ProhibitedModifiers,
    AbstractScore.EModifiers MandatoryModifiers,
    // On the Request, make sure its null or > 0 and <= 100
    Accuracy? MinAccuracy
);

public class RankedMapRequirementsConfiguration : IComplexPropertyConfiguration<RankedMapRequirements>
{
    public ComplexPropertyBuilder<RankedMapRequirements> Configure(
        ComplexPropertyBuilder<RankedMapRequirements> builder)
    {
        builder.Property(x => x.MinAccuracy)
            .HasConversion<float?>(from => from, to => Accuracy.CreateUnsafe(to));

        return builder;
    }
}