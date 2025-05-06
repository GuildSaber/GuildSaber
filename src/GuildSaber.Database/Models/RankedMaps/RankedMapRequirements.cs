using GuildSaber.Database.Models.Scores;

namespace GuildSaber.Database.Models.RankedMaps;

public readonly record struct RankedMapRequirements(
    bool NeedConfirmation,
    bool NeedFullCombo,
    float MaxPauseDuration,
    AbstractScore.EModifiers ProhibitedModifiers,
    AbstractScore.EModifiers MandatoryModifiers,
    float MinAccuracy
);