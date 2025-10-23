using GuildSaber.Common.Helpers;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
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
        BaseScore baseScore, EModifiers modifiers, ModifierValues modifierValues)
        => modifiers.GetFlags()
                .Aggregate(seed: 1f, (percentage, mod) => percentage + mod switch
                {
                    EModifiers.None => 0,
                    EModifiers.OffPlatform => modifierValues.OffPlatform,
                    EModifiers.NoFail => modifierValues.NoFail,
                    EModifiers.NoObstacles => modifierValues.NoObstacles,
                    EModifiers.NoBombs => modifierValues.NoBombs,
                    EModifiers.SlowerSong => modifierValues.SlowerSong,
                    EModifiers.NoArrows => modifierValues.NoArrows,
                    EModifiers.BatteryEnergy => modifierValues.BatteryEnergy,
                    EModifiers.InstaFail => modifierValues.InstaFail,
                    EModifiers.SmallNotes => modifierValues.SmallNotes,
                    EModifiers.ProMode => modifierValues.ProMode,
                    EModifiers.StrictAngles => modifierValues.StrictAngles,
                    EModifiers.OldDots => modifierValues.OldDots,
                    EModifiers.FasterSong => modifierValues.FasterSong,
                    EModifiers.DisappearingArrows => modifierValues.DisappearingArrows,
                    EModifiers.GhostNotes => modifierValues.GhostNotes,
                    EModifiers.SuperFastSong => modifierValues.SuperFastSong,
                    EModifiers.Unk => 0,
                    EModifiers.ProhibitedDefaults => 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(mod), mod, null)
                }) switch
            {
                < 0.0f => EffectiveScore.CreateUnsafe(0).Value,
                var adder => EffectiveScore.CreateUnsafe((int)(baseScore * adder)).Value
            };

    public static (EState, EDenyReason) RecalculateStateAndReason(
        EState state, AbstractScore score, RankedMapRequirements requirements, SongDifficultyStats songDifficultyStats)
    {
        var denyReason = EDenyReason.Unspecified;

        if (requirements.NeedFullCombo && !score.IsFullCombo)
            denyReason |= EDenyReason.NoFullCombo;

        if (requirements.MaxPauseDurationSec is not null)
        {
            if (score is not BeatLeaderScore blScore)
                denyReason |= EDenyReason.MissingTrackers;
            else if (blScore.ScoreStatistics?.WinTracker.TotalPauseDuration > requirements.MaxPauseDurationSec)
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

    public static RawPoints CalculateRawPoints(
        BaseScore baseScore,
        EffectiveScore effectiveScore,
        MaxScore maxScore,
        Point point,
        RankedMapRating rating)
    {
        var accuracyPoints = rating.AccStar * point.CurveSettings.Accuracy.ProjectValue(
            Accuracy.From(baseScore, maxScore)
        );

        var difficultyPoints = point.CurveSettings.Difficulty.ProjectValue(
            rating.DiffStar
        );

        return RawPoints.TryCreate((float)(accuracyPoints + difficultyPoints)).Unwrap();
    }

    /// <summary>
    /// Infers the stars depending on the required modifiers and the ExMachina.
    /// </summary>
    /// <returns>
    /// The inferred accuracy and difficulty stars.
    /// </returns>
    public static (RankedMapRating.AccuracyStar, RankedMapRating.DifficultyStar) StarsFromExMachina(
        ExMachinaResponse exMachina, RankedMapRequirements requirements, CustomCurve accCurve)
        => requirements.MandatoryModifiers switch
        {
            var mods when mods.HasFlag(EModifiers.SlowerSong)
                => (exMachina.SS.ToAccStar(accCurve), exMachina.SS.ToDiffStar()),
            var mods when mods.HasFlag(EModifiers.FasterSong)
                => (exMachina.FS.ToAccStar(accCurve), exMachina.FS.ToDiffStar()),
            var mods when mods.HasFlag(EModifiers.SuperFastSong)
                => (exMachina.SFS.ToAccStar(accCurve), exMachina.SFS.ToDiffStar()),
            _ => (exMachina.None.ToAccStar(accCurve), exMachina.None.ToDiffStar())
        };

    public static RankedMapRating.AccuracyStar ToAccStar(this RatingResult rating, CustomCurve accCurve)
        => new(Inflate(GetPP(AccRating(rating, accCurve), rating, accCurve)));

    public static RankedMapRating.DifficultyStar ToDiffStar(this RatingResult rating)
        => new(Inflate(PassRating(rating.LackMapCalculation.PassRating) * (rating.LackMapCalculation.TechRating * 10)) /
               13f);

    /// <summary>
    /// Original formula from BeatLeader
    /// </summary>
    private static float PassRating(float original) => original < 24.4 ? original : 16 + MathF.Sqrt(original) * 1.7f;

    /// <remarks>
    /// The logic behind this formula is old and is simply a port of the old bot logic.
    /// Refinements and logic changes are totally expected in the future.
    /// </remarks>
    public static float AccRating(RatingResult rating, CustomCurve curve)
    {
        var passRating = PassRating(rating.LackMapCalculation.PassRating);
        var techRating = rating.LackMapCalculation.TechRating * 10;
        float difficultyToAcc;

        if (rating.PredictedAcc > 0)
        {
            difficultyToAcc = 15f / (float)curve.ProjectValue(rating.PredictedAcc + 0.0022d);
        }
        else
        {
            var tinyTech = 0.0208f * techRating + 1.1284f;
            difficultyToAcc = (-MathF.Pow(tinyTech, -passRating) + 1)
                * 8 + 2 + 0.01f * techRating * passRating;
        }

        if (float.IsInfinity(difficultyToAcc) || float.IsNaN(difficultyToAcc) ||
            float.IsNegativeInfinity(difficultyToAcc))
            difficultyToAcc = 0;

        return difficultyToAcc;
    }

    /// <summary>
    /// BeatLeader's formula equivalent (ONLY used to calculate the star rating in our case)
    /// </summary>
    private static float GetPP(float accRating, RatingResult rating, CustomCurve curve)
    {
        const float ppAtPercentage = 0.96f;
        var passRating = PassRating(rating.LackMapCalculation.PassRating);
        var techRating = rating.LackMapCalculation.TechRating * 10;

        var passPP = 15.2f * MathF.Exp(MathF.Pow(passRating, 1 / 2.62f)) - 30f;

        if (float.IsInfinity(passPP) || float.IsNaN(passPP) || float.IsNegativeInfinity(passPP) || passPP < 0)
            passPP = 0;

        var accPP = (float)curve.ProjectValue(ppAtPercentage) * accRating * 34f;
        var techPP = MathF.Exp(1.9f * ppAtPercentage) * 1.08f * techRating;

        return passPP + accPP + techPP;
    }

    private static float Inflate(float pp)
        => 650f * MathF.Pow(pp, 1.3f) / MathF.Pow(650f, 1.3f);
}