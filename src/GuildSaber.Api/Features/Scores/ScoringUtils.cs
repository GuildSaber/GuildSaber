using GuildSaber.Common.Helpers;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using EModifiers = GuildSaber.Database.Models.Server.Scores.AbstractScore.EModifiers;
using EState = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EState;
using EDenyReason = GuildSaber.Database.Models.Server.RankedScores.RankedScore.EDenyReason;

namespace GuildSaber.Api.Features.Scores;

/// <summary>
/// Code part and magic numbers from https://github.com/BeatLeader/beatleader-server/
/// </summary>
public static class ScoringUtils
{
    public static EffectiveScore CalculateScoreFromModifiers(
        BaseScore baseScore, EModifiers modifiers, ModifierSettings modifierSettings)
        => modifiers.GetFlags()
                .Aggregate(seed: 1f, (percentage, mod) => percentage + mod switch
                {
                    EModifiers.None => 0,
                    EModifiers.OffPlatform => modifierSettings.OffPlatform,
                    EModifiers.NoFail => modifierSettings.NoFail,
                    EModifiers.NoObstacles => modifierSettings.NoObstacles,
                    EModifiers.NoBombs => modifierSettings.NoBombs,
                    EModifiers.SlowerSong => modifierSettings.SlowerSong,
                    EModifiers.NoArrows => modifierSettings.NoArrows,
                    EModifiers.BatteryEnergy => modifierSettings.BatteryEnergy,
                    EModifiers.InstaFail => modifierSettings.InstaFail,
                    EModifiers.SmallNotes => modifierSettings.SmallNotes,
                    EModifiers.ProMode => modifierSettings.ProMode,
                    EModifiers.StrictAngles => modifierSettings.StrictAngles,
                    EModifiers.OldDots => modifierSettings.OldDots,
                    EModifiers.FasterSong => modifierSettings.FasterSong,
                    EModifiers.DisappearingArrows => modifierSettings.DisappearingArrows,
                    EModifiers.GhostNotes => modifierSettings.GhostNotes,
                    EModifiers.SuperFastSong => modifierSettings.SuperFastSong,
                    EModifiers.Unk => 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(mod), mod, null)
                }) switch
            {
                < 0.0f => EffectiveScore.CreateUnsafe(0).Value,
                var adder => EffectiveScore.CreateUnsafe((ulong)(baseScore * adder)).Value
            };

    public static (EState, EDenyReason) RecalculateStateAndReason(
        EState state, AbstractScore score, RankedMapRequirements requirements, SongDifficultyStats songDifficultyStats)
    {
        var denyReason = EDenyReason.Unspecified;

        if (requirements.NeedFullCombo && !score.IsFullCombo)
            denyReason |= EDenyReason.NoFullCombo;

        if (requirements.MaxPauseDuration is not null)
        {
            if (score is not BeatLeaderScore blScore)
                denyReason |= EDenyReason.MissingTrackers;
            else if (blScore.ScoreStatistics?.WinTracker.TotalPauseDuration > requirements.MaxPauseDuration)
                denyReason |= EDenyReason.TooMuchPaused;
        }

        if (requirements.MinAccuracy is not null
            && Accuracy.From(score.BaseScore, songDifficultyStats.MaxScore) < requirements.MinAccuracy)
            denyReason |= EDenyReason.MinAccuracyRequirements;

        if (requirements.MandatoryModifiers != EModifiers.None
            && !score.Modifiers.HasFlag(requirements.MandatoryModifiers))
            denyReason |= EDenyReason.MissingModifiers;

        if (score.Modifiers.HasAnyFlag(requirements.ProhibitedModifiers))
            denyReason |= EDenyReason.ProhibitedModifiers;

        /* Set the state to Denied if we did find a reason to deny previously.
         * This even overrides Confirmed/Refused to allow a score to get the chance to get through verification again
         * if requirements changes. */
        if (denyReason != EDenyReason.Unspecified)
            state = EState.Denied;

        /* Keep NeedConfirmation last so it doesn't override other states.
         * The Need confirmation / Pending state will only be set if the score is not already in a final state.
         * (e.g. Denied, Refused, Confirmed) (Btw: Setting Pending to an already Pending score doesn't matter here.) */
        if (requirements.NeedConfirmation &&
            (state == EState.None || !state.HasAnyFlag(EState.Denied | EState.Refused | EState.Confirmed)))
            state |= EState.Pending;

        return (state, denyReason);
    }

    public static RawPoints CalculateRawPoints(EffectiveScore effectiveScore, Point point, RankedMapRating rating)
        => throw new NotImplementedException();
}