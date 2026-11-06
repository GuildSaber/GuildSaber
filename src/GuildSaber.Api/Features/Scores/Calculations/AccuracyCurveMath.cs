using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Api.Features.Scores.Calculations;

/// <summary>Normalized accuracy fractions mapped to fractions of a point's accuracy bonus.</summary>
public static class AccuracyCurveMath
{
    public const int MaximumPoints = 256;
    public const double MinimumSamplingTolerance = 1e-12;

    public static Result<CustomCurve> Validate(CurvePoint[]? samples, bool requireMonotone = false)
    {
        if (samples is null || samples.Length is < 2 or > MaximumPoints)
            return Failure<CustomCurve>("An accuracy curve requires 2..256 samples.");

        var points = (CurvePoint[])samples.Clone();
        foreach (var (x, y) in points)
            if (!double.IsFinite(x) || !double.IsFinite(y) || x is < 0 or > 1 || y is < 0 or > 1)
                return Failure<CustomCurve>("Accuracy curve coordinates must be finite fractions in [0, 1].");

        var descending = points[0].X > points[^1].X;
        for (var index = 1; index < points.Length; index++)
            if (descending ? points[index - 1].X <= points[index].X : points[index - 1].X >= points[index].X)
                return Failure<CustomCurve>(
                    "Accuracy sample X coordinates must be strictly ordered without duplicates.");

        if (!descending) Array.Reverse(points);
        if (!points[0].X.Equals(1d) || !points[^1].X.Equals(0d))
            return Failure<CustomCurve>("Accuracy curves must cover both X = 0 and X = 1.");

        if (!requireMonotone) return new CustomCurve(points);

        if (!points[0].Y.Equals(1d) || !points[^1].Y.Equals(0d))
            return Failure<CustomCurve>("A native accuracy curve must start at (0, 0) and end at (1, 1).");

        for (var index = 1; index < points.Length; index++)
            if (points[index - 1].Y < points[index].Y)
                return Failure<CustomCurve>("A native accuracy curve must be nondecreasing with accuracy.");

        return new CustomCurve(points);
    }

    /// <summary>Evaluate a validated descending curve at a validated accuracy, without allocations.</summary>
    public static double Evaluate(CustomCurve curve, Accuracy accuracy)
        => EvaluateFraction(curve, accuracy / 100d);

    internal static double EvaluateFraction(CustomCurve curve, double accuracy)
    {
        var points = curve.Points;
        var left = 0;
        var right = points.Length - 1;
        while (right - left > 1)
        {
            var middle = left + (right - left) / 2;
            if (points[middle].X.Equals(accuracy)) return points[middle].Y;
            if (accuracy > points[middle].X) right = middle;
            else left = middle;
        }

        if (accuracy.Equals(points[left].X)) return points[left].Y;
        if (accuracy.Equals(points[right].X)) return points[right].Y;
        return points[left].Y + (points[right].Y - points[left].Y)
            * ((accuracy - points[left].X) / (points[right].X - points[left].X));
    }

    public static Result<double> MaximumReward(double basePoints, double bonusRatio)
        => !double.IsFinite(basePoints) || basePoints < 0 || !double.IsFinite(bonusRatio) || bonusRatio < 0
           || !double.IsFinite(basePoints * (1 + bonusRatio)) || basePoints * (1 + bonusRatio) > float.MaxValue
            ? Failure<double>("The base reward and maximum bonus must be finite, nonnegative RawPoints values.")
            : basePoints * (1 + bonusRatio);

    public static Result<RawPoints> Calculate(
        double basePoints, double bonusRatio, Accuracy accuracy, CustomCurve? accuracyCurve)
    {
        if (MaximumReward(basePoints, bonusRatio).TryGetError(out var error))
            return Failure<RawPoints>(error);

        if (bonusRatio == 0 || basePoints == 0)
            return RawPoints.TryCreate((float)basePoints);

        if (accuracyCurve is null)
            return Failure<RawPoints>("An accuracy curve is required for a positive bonus budget.");

        var total = basePoints + basePoints * bonusRatio * Evaluate(accuracyCurve, accuracy);
        return !double.IsFinite(total) || total < 0 || total > float.MaxValue
            ? Failure<RawPoints>("The resulting reward is outside the RawPoints range.")
            : RawPoints.TryCreate((float)total);
    }
}