using GuildSaber.Database.Models.Server.Scores;

namespace GuildSaber.Database.Models.Server.RankedMaps;

public readonly record struct RankedMapRequirements(
    bool NeedConfirmation,
    bool NeedFullCombo,
    float MaxPauseDuration,
    AbstractScore.EModifiers ProhibitedModifiers,
    AbstractScore.EModifiers MandatoryModifiers,
    float MinAccuracy
);