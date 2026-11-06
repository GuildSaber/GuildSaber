using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.Server.Guilds.Points;

namespace GuildSaber.Api.Features.Scores.Calculations;

/// <summary>Produces sampled linear curves from local-context benchmark accuracy fractions.</summary>
public static class NativeCurveGenerator
{
    public const int MaximumRunnerUps = 10;
    private const int MaximumAnchors = MaximumRunnerUps + 3;

    public sealed record GeneratedCurve(CustomCurve Curve, CurvePoint[] Anchors, double MaximumError);

    public static Result<GeneratedCurve> Generate(
        double[] accuracies, double[] runnerUpFractions, double tolerance,
        int maximumPoints = AccuracyCurveMath.MaximumPoints)
    {
        if (runnerUpFractions.Length > MaximumRunnerUps
            || accuracies.Length == 0
            || accuracies.Length > runnerUpFractions.Length + 1)
            return Failure<GeneratedCurve>(
                "Generation needs a leader and at most the configured number of runners-up.");

        if (!double.IsFinite(tolerance)
            || tolerance < AccuracyCurveMath.MinimumSamplingTolerance
            || tolerance > 1
            || maximumPoints is < 2 or > AccuracyCurveMath.MaximumPoints)
            return Failure<GeneratedCurve>(
                "Sampling requires a finite tolerance in [1e-12, 1] and a point limit in 2..256.");

        var previousReward = 1d;
        foreach (var reward in runnerUpFractions)
        {
            if (!double.IsFinite(reward) || reward < 0 || reward > previousReward)
                return Failure<GeneratedCurve>(
                    "Runner-up fractions must be non-increasing finite fractions in [0, 1].");
            previousReward = reward;
        }

        Span<CurvePoint> anchors = stackalloc CurvePoint[MaximumAnchors];
        anchors[0] = new CurvePoint(0, 0);
        var anchorCount = 1;

        // Iterate from the lowest benchmark upward. Ties keep the highest reward, including the leader's 1.
        for (var index = accuracies.Length - 1; index >= 0; index--)
        {
            var accuracy = accuracies[index];
            if (!double.IsFinite(accuracy) || accuracy <= 0 || accuracy > 1
                || index > 0 && accuracies[index - 1] < accuracy)
                return Failure<GeneratedCurve>("Benchmark accuracies must be ordered descending and lie in (0, 1].");

            var anchor = new CurvePoint(accuracy, index == 0 ? 1 : runnerUpFractions[index - 1]);

            if (anchors[anchorCount - 1].X.Equals(accuracy)) anchors[anchorCount - 1] = anchor;
            else anchors[anchorCount++] = anchor;
        }

        if (!anchors[anchorCount - 1].X.Equals(1d))
            anchors[anchorCount++] = new CurvePoint(1, 1);

        var canonicalAnchors = anchors[..anchorCount];

        if (!PchipSampler.Sample(canonicalAnchors, tolerance, maximumPoints)
                .TryGetValue(out var sampled, out var error))
            return Failure<GeneratedCurve>(error);

        return new GeneratedCurve(sampled.Curve, [.. canonicalAnchors], sampled.MaximumError);
    }
}

/// <summary>
/// Fritsch-Butland harmonic interior slopes and shape-constrained one-sided endpoint slopes.
/// Cubics use a local unit coordinate, avoiding expansion in absolute accuracy coordinates.
/// </summary>
internal ref struct PchipSampler(Span<CurvePoint> samples, double tolerance)
{
    private const int MaximumSubdivisionDepth = 48;

    private readonly Span<CurvePoint> _samples = samples;
    private int _sampleCount = 1;
    private double _maximumError;

    private readonly Span<CurvePoint> Points => _samples[.._sampleCount];

    internal readonly record struct Segment(double X, double Width, double Y, double C1, double C2, double C3)
    {
        public double Evaluate(double t) => Y + t * (C1 + t * (C2 + t * C3));

        // Cubic minus chord on t = start + (end-start)*u is A*(u^3-u)+B*(u^2-u).
        // Check every stationary point, including the midpoint-zero inflection case.
        public double ChordError(double start, double end)
        {
            var width = end - start;
            var a = C3 * width * width * width;
            var b = (C2 + 3 * C3 * start) * width * width;
            var c = -a - b;

            if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c))
                return double.NaN;

            var maximum = 0d;
            if (a == 0)
            {
                if (b != 0) Include(-c / (2 * b));
            }
            else
            {
                var discriminant = 4 * b * b - 12 * a * c;
                if (!double.IsFinite(discriminant)) return double.NaN;
                if (discriminant < 0) return maximum;

                var root = Math.Sqrt(discriminant);
                // The q form avoids subtracting almost equal values for one root.
                var q = -b - Math.CopySign(root / 2, b);
                if (q == 0) Include(-b / (3 * a));
                else
                {
                    Include(q / (3 * a));
                    Include(c / q);
                }
            }

            return maximum;

            void Include(double u)
            {
                if (!double.IsFinite(u))
                {
                    maximum = double.NaN;
                    return;
                }

                if (u is > 0 and < 1)
                    maximum = Math.Max(maximum, Math.Abs(u * (u - 1) * (a * (u + 1) + b)));
            }
        }
    }

    internal readonly record struct SampledCurve(CustomCurve Curve, double MaximumError);

    internal static Result<SampledCurve> Sample(ReadOnlySpan<CurvePoint> anchors, double tolerance, int maximumPoints)
    {
        if (maximumPoints is < 2 or > AccuracyCurveMath.MaximumPoints
            || anchors.Length < 2
            || anchors.Length > maximumPoints)
            return Failure<SampledCurve>("The sample limit cannot preserve all generation anchors.");

        Span<double> tangents = stackalloc double[anchors.Length];
        var derivatives = CalculateTangents(anchors, tangents);

        if (derivatives.IsFailure)
            return Failure<SampledCurve>(derivatives.Error);

        Span<CurvePoint> points = stackalloc CurvePoint[maximumPoints];
        points[0] = anchors[0];
        var sampler = new PchipSampler(points, tolerance);

        for (var index = 0; index < anchors.Length - 1; index++)
        {
            var width = anchors[index + 1].X - anchors[index].X;
            var difference = anchors[index + 1].Y - anchors[index].Y;
            var m0 = width * tangents[index];
            var m1 = width * tangents[index + 1];
            var segment = new Segment(anchors[index].X, width, anchors[index].Y,
                m0, 3 * difference - 2 * m0 - m1, -2 * difference + m0 + m1);

            if (!double.IsFinite(segment.C1) || !double.IsFinite(segment.C2) || !double.IsFinite(segment.C3))
                return Failure<SampledCurve>("Generation coefficients are outside the finite numerical range.");

            if (!sampler.Append(segment, 0, 1, 0, anchors[index + 1]))
                return Failure<SampledCurve>("The point/work limit cannot meet the normalized sampling tolerance.");
        }

        sampler.Points.Reverse();

        return new SampledCurve(new CustomCurve([.. sampler.Points]), sampler._maximumError);
    }

    private static Result CalculateTangents(ReadOnlySpan<CurvePoint> anchors, Span<double> tangents)
    {
        var count = anchors.Length;
        Span<double> widths = stackalloc double[count - 1];
        Span<double> slopes = stackalloc double[count - 1];

        for (var index = 0; index < count - 1; index++)
        {
            widths[index] = anchors[index + 1].X - anchors[index].X;
            slopes[index] = (anchors[index + 1].Y - anchors[index].Y) / widths[index];

            if (widths[index] <= 0 || !double.IsFinite(slopes[index]))
                return Failure("Generation anchors are too close to represent safely.");
        }

        // Assign every tangent: stack storage is uninitialized, including the entries for flat intervals.
        if (count == 2) tangents[0] = tangents[1] = slopes[0];
        else
        {
            tangents[0] = Endpoint(widths[0], widths[1], slopes[0], slopes[1]);
            tangents[^1] = Endpoint(widths[^1], widths[^2], slopes[^1], slopes[^2]);
            for (var index = 1; index < count - 1; index++)
            {
                if (slopes[index - 1] == 0 || slopes[index] == 0)
                {
                    tangents[index] = 0;
                    continue;
                }

                var w1 = 2 * widths[index] + widths[index - 1];
                var w2 = widths[index] + 2 * widths[index - 1];
                tangents[index] = (w1 + w2) / (w1 / slopes[index - 1] + w2 / slopes[index]);
            }
        }

        foreach (var tangent in tangents)
        {
            if (!double.IsFinite(tangent))
                return Failure("Generation derivatives are outside the finite numerical range.");
        }

        return Success();
    }

    private bool Append(Segment segment, double start, double end, int depth, CurvePoint right)
    {
        var error = segment.ChordError(start, end);

        if (!double.IsFinite(error)) return false;

        if (error <= tolerance)
        {
            if (_sampleCount >= _samples.Length) return false;

            _samples[_sampleCount++] = right;
            _maximumError = Math.Max(_maximumError, error);
            return true;
        }

        var middleX = segment.X + segment.Width * (start + (end - start) / 2);

        // The persisted X is rounded. Its actual local coordinate, rather than the ideal midpoint,
        // must define both Y and the child intervals used by the error proof.
        var middle = (middleX - segment.X) / segment.Width;

        if (depth >= MaximumSubdivisionDepth || middle.Equals(start) || middle.Equals(end)
            || middleX <= _samples[_sampleCount - 1].X || middleX >= right.X || _sampleCount >= _samples.Length - 1)
            return false;

        var midpoint = new CurvePoint(middleX, segment.Evaluate(middle));

        if (!double.IsFinite(midpoint.Y)) return false;

        return Append(segment, start, middle, depth + 1, midpoint)
               && Append(segment, middle, end, depth + 1, right);
    }

    private static double Endpoint(double h0, double h1, double d0, double d1)
    {
        var derivative = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
        if (Math.Sign(derivative) != Math.Sign(d0)) return 0;

        return Math.Sign(d0) != Math.Sign(d1) && Math.Abs(derivative) > 3 * Math.Abs(d0)
            ? 3 * d0
            : derivative;
    }
}
