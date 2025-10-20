using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.RankedMaps;

public static class RankedMapMappers
{
    public static Result<RankedMapRequirements, List<KeyValuePair<string, string[]>>> Map(
        this RankedMapRequest.RankedMapRequirements self)
    {
        List<KeyValuePair<string, string[]>> validationErrors = [];
        var accuracyResult = self.MinAccuracy.HasValue
            ? Accuracy.TryCreate(self.MinAccuracy.Value)
            : null as Result<Accuracy>?;

        var accuracy = default(Accuracy);
        if (accuracyResult is not null && !accuracyResult.Value.TryGetValue(out accuracy, out var accError))
            validationErrors.Add(new KeyValuePair<string, string[]>("MinAccuracy", [accError]));

        var prohibitedModifiersResult = self.ProhibitedModifiers.Map();
        if (!prohibitedModifiersResult.TryGetValue(out var prohibitedModifiers, out var probModError))
            validationErrors.Add(new KeyValuePair<string, string[]>("ProhibitedModifiers", [probModError]));

        var mandatoryModifiersResult = self.MandatoryModifiers.Map();
        if (!mandatoryModifiersResult.TryGetValue(out var mandatoryModifiers, out var mandModError))
            validationErrors.Add(new KeyValuePair<string, string[]>("MandatoryModifiers", [mandModError]));

        if (validationErrors.Count > 0)
            return Failure<RankedMapRequirements, List<KeyValuePair<string, string[]>>>(validationErrors);

        return new RankedMapRequirements(
            MinAccuracy: accuracyResult is null ? null : accuracy,
            ProhibitedModifiers: prohibitedModifiers,
            MandatoryModifiers: mandatoryModifiers,
            NeedConfirmation: self.NeedConfirmation,
            NeedFullCombo: self.NeedFullCombo,
            MaxPauseDurationSec: self.MaxPauseDurationSec
        );
    }

    public static Result<AbstractScore.EModifiers> Map(this RankedMapRequest.EModifiers self) => self switch
    {
        RankedMapRequest.EModifiers.None => AbstractScore.EModifiers.None,
        RankedMapRequest.EModifiers.NoObstacles => AbstractScore.EModifiers.NoObstacles,
        RankedMapRequest.EModifiers.NoBombs => AbstractScore.EModifiers.NoBombs,
        RankedMapRequest.EModifiers.NoFail => AbstractScore.EModifiers.NoFail,
        RankedMapRequest.EModifiers.SlowerSong => AbstractScore.EModifiers.SlowerSong,
        RankedMapRequest.EModifiers.BatteryEnergy => AbstractScore.EModifiers.BatteryEnergy,
        RankedMapRequest.EModifiers.InstaFail => AbstractScore.EModifiers.InstaFail,
        RankedMapRequest.EModifiers.SmallNotes => AbstractScore.EModifiers.SmallNotes,
        RankedMapRequest.EModifiers.ProMode => AbstractScore.EModifiers.ProMode,
        RankedMapRequest.EModifiers.FasterSong => AbstractScore.EModifiers.FasterSong,
        RankedMapRequest.EModifiers.StrictAngles => AbstractScore.EModifiers.StrictAngles,
        RankedMapRequest.EModifiers.DisappearingArrows => AbstractScore.EModifiers.DisappearingArrows,
        RankedMapRequest.EModifiers.GhostNotes => AbstractScore.EModifiers.GhostNotes,
        RankedMapRequest.EModifiers.NoArrows => AbstractScore.EModifiers.NoArrows,
        RankedMapRequest.EModifiers.SuperFastSong => AbstractScore.EModifiers.SuperFastSong,
        RankedMapRequest.EModifiers.OldDots => AbstractScore.EModifiers.OldDots,
        RankedMapRequest.EModifiers.OffPlatform => AbstractScore.EModifiers.OffPlatform,
        RankedMapRequest.EModifiers.ProhibitedDefaults => AbstractScore.EModifiers.ProhibitedDefaults,
        _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
}