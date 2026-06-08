using GuildSaber.Common.Helpers;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.StrongTypes;
using EModifiers = GuildSaber.Database.Models.Server.Scores.AbstractScore.EModifiers;
using EInvalidReason = GuildSaber.Database.Models.Server.RankedScores.InvalidRankedScore.EInvalidReason;

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

    public static RankedScore.ERankedScoreType RecalculateRankedScoreType(
        RankedScore? rankedScore,
        AbstractScore score,
        RankedMapRequirements requirements,
        SongDifficultyStats songDifficultyStats,
        out EInvalidReason invalidReason)
    {
        invalidReason = CalculateInvalidReason(score, requirements, songDifficultyStats);

        if (invalidReason != EInvalidReason.Unspecified)
            return RankedScore.ERankedScoreType.Invalid;

        return rankedScore switch
        {
            AcceptedRankedScore => RankedScore.ERankedScoreType.Accepted,
            RefusedRankedScore => RankedScore.ERankedScoreType.Refused,
            _ when requirements.NeedConfirmation => RankedScore.ERankedScoreType.Pending,
            _ => RankedScore.ERankedScoreType.Valid
        };
    }

    private static EInvalidReason CalculateInvalidReason(
        AbstractScore score, RankedMapRequirements requirements, SongDifficultyStats songDifficultyStats)
    {
        var invalidReason = EInvalidReason.Unspecified;

        if (requirements.NeedFullCombo && !score.IsFullCombo)
            invalidReason |= EInvalidReason.NoFullCombo;

        if (requirements.MaxPauseDurationSec is not null)
        {
            if (score is not BeatLeaderScore blScore)
                invalidReason |= EInvalidReason.MissingTrackers;
            else if (blScore.Statistics?.WinTracker.TotalPauseDuration > requirements.MaxPauseDurationSec)
                invalidReason |= EInvalidReason.PausedTooMuch;
        }

        if (requirements.MinAccuracy is not null
            && Accuracy.From(score.BaseScore, songDifficultyStats.MaxScore) < requirements.MinAccuracy)
            invalidReason |= EInvalidReason.MinAccuracyRequirements;

        if (requirements.MandatoryModifiers != EModifiers.None
            && !score.Modifiers.HasFlag(requirements.MandatoryModifiers))
            invalidReason |= EInvalidReason.MissingModifiers;

        if (score.Modifiers.HasAnyFlag(requirements.ProhibitedModifiers))
            invalidReason |= EInvalidReason.ProhibitedModifiers;

        return invalidReason;
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