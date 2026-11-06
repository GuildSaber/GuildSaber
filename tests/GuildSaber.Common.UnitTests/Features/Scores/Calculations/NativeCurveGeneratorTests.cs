using AwesomeAssertions;
using GuildSaber.Api.Features.Scores.Calculations;
using GuildSaber.Database.Models.Server.Guilds.Points;
using JetBrains.Annotations;

namespace GuildSaber.Common.UnitTests.Features.Scores.Calculations;

[PublicAPI]
public class NativeCurveGeneratorTests
{
    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void Benchmarks_ShouldPreserveAnchorsTiesAndFlatUpperTail()
    {
        var result = NativeCurveGenerator.Generate([0.98, 0.98, 0.95, 0.9], [0.9, 0.8, 0.7], 0.001).Value;
        AccuracyCurveMath.EvaluateFraction(result.Curve, 0.98).Should().Be(1);
        AccuracyCurveMath.EvaluateFraction(result.Curve, 0.99).Should().Be(1);
        AccuracyCurveMath.EvaluateFraction(result.Curve, 0.95).Should().Be(0.8);
        AccuracyCurveMath.EvaluateFraction(result.Curve, 0.9).Should().Be(0.7);
        result.Anchors.Should().HaveCount(5);
        result.MaximumError.Should().BeLessThanOrEqualTo(0.001);
        AccuracyCurveMath.Validate(result.Curve.Points, requireMonotone: true).IsSuccess.Should().BeTrue();
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void OnePlayerAndCollinearCases_ShouldGenerateWithoutInventingRunnerUps()
    {
        var one = NativeCurveGenerator.Generate([0.9], [0.9, 0.8, 0.7], 0.001).Value;
        one.Anchors.Should().Equal(new CurvePoint(0, 0), new CurvePoint(0.9, 1), new CurvePoint(1, 1));
        AccuracyCurveMath.EvaluateFraction(one.Curve, 0.95).Should().Be(1);
        var line = NativeCurveGenerator.Generate([1, 0.5], [0.5], 1e-12).Value;
        line.Curve.Points.Should().HaveCount(3);
        line.MaximumError.Should().Be(0);
        var repeated = NativeCurveGenerator.Generate([1, 1, 1], [0.9, 0.8], 0.001).Value;
        repeated.Curve.Points.Should().HaveCount(2);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void Sampling_ShouldAgreeWithIndependentHermiteReferenceAcrossSharpBends()
    {
        var generated = NativeCurveGenerator.Generate([0.999, 0.9989, 0.7, 0.1], [0.95, 0.4, 0.01], 0.0001).Value;
        var anchors = generated.Anchors;
        var derivatives = ReferenceDerivatives(anchors);
        var maximumError = 0d;
        for (var segment = 0; segment < anchors.Length - 1; segment++)
        for (var sample = 0; sample <= 3000; sample++)
        {
            var t = sample / 3000d;
            var a = anchors[segment];
            var b = anchors[segment + 1];
            var h = b.X - a.X;
            // Independent Hermite basis evaluation, rather than the sampler's polynomial coefficients.
            var reference = (2 * t * t * t - 3 * t * t + 1) * a.Y
                            + (t * t * t - 2 * t * t + t) * h * derivatives[segment]
                            + (-2 * t * t * t + 3 * t * t) * b.Y
                            + (t * t * t - t * t) * h * derivatives[segment + 1];
            var actual = AccuracyCurveMath.EvaluateFraction(generated.Curve, a.X + h * t);
            maximumError = Math.Max(maximumError, Math.Abs(actual - reference));
        }
        maximumError.Should().BeLessThanOrEqualTo(0.0001 + 1e-12);
        generated.MaximumError.Should().BeGreaterThanOrEqualTo(maximumError - 1e-12);
        NativeCurveGenerator.Generate([0.999, 0.9989, 0.7, 0.1], [0.95, 0.4, 0.01], 0.0001)
            .Value.Curve.Points.Should().Equal(generated.Curve.Points);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void ErrorExtrema_ShouldDetectDeviationWhenMidpointIsExactlyOnChord()
    {
        // 3t^2 - 2t^3: chord difference is zero at 0, 0.5 and 1, but extrema are ±sqrt(3)/18.
        var segment = new PchipSampler.Segment(0, 1, 0, 0, 3, -2);
        segment.Evaluate(0.5).Should().Be(0.5);
        segment.ChordError(0, 1).Should().BeApproximately(Math.Sqrt(3) / 18, 1e-14);
        new PchipSampler.Segment(0, 1, 0, 0, 1, 0).ChordError(0, 1)
            .Should().BeApproximately(0.25, 1e-14);
        new PchipSampler.Segment(0, 1, 0, 1, 0, 0).ChordError(0, 1).Should().Be(0);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void CloseBenchmarks_ShouldCertifyTheActualRoundedSampleCoordinates()
    {
        var (curve, anchors, certifiedError) = NativeCurveGenerator.Generate([0.98000000000001, 0.98], [0.5], 0.001).Value;
        var derivatives = ReferenceDerivatives(anchors);
        var lower = anchors[1];
        var upper = anchors[2];
        var width = upper.X - lower.X;
        var maximumError = 0d;
        for (var accuracy = lower.X; accuracy <= upper.X; accuracy = Math.BitIncrement(accuracy))
        {
            var t = (accuracy - lower.X) / width;
            var reference = (2 * t * t * t - 3 * t * t + 1) * lower.Y
                            + (t * t * t - 2 * t * t + t) * width * derivatives[1]
                            + (-2 * t * t * t + 3 * t * t) * upper.Y
                            + (t * t * t - t * t) * width * derivatives[2];
            maximumError = Math.Max(maximumError,
                Math.Abs(AccuracyCurveMath.EvaluateFraction(curve, accuracy) - reference));
        }
        maximumError.Should().BeLessThanOrEqualTo(0.001 + 1e-12);
        maximumError.Should().BeLessThanOrEqualTo(certifiedError + 1e-12);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void LimitsAndInvalidInputs_ShouldReturnExplicitFailure()
    {
        foreach (var maximumPoints in new[] { 0, 1, AccuracyCurveMath.MaximumPoints + 1 })
            NativeCurveGenerator.Generate([1], [], 0.001, maximumPoints).IsFailure.Should().BeTrue();

        NativeCurveGenerator.Generate([0.9], [], 1e-9, maximumPoints: 3).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([], [0.9, 0.8, 0.7], 0.001).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([0.9, 0.95], [0.9], 0.001).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([0.9], [0.7, 0.9], 0.001).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([0.9], [double.NaN], 0.001).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([0.9], [], 0).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([0.9], [], 1e-15).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([double.NaN], [], 0.001).IsFailure.Should().BeTrue();
        NativeCurveGenerator.Generate([1e-308, 5e-309], [0.5], 0.001).IsFailure.Should().BeTrue();
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void MaximumBenchmarks_ShouldPreserveAnchors()
    {
        double[] accuracies = [0.99, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1, 0.01];
        double[] rewards = [0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1, 0];
        var generated = NativeCurveGenerator.Generate(accuracies, rewards, 0.001).Value;
        generated.Anchors.Should().HaveCount(NativeCurveGenerator.MaximumRunnerUps + 3);
        AccuracyCurveMath.Validate(generated.Curve.Points, requireMonotone: true).IsSuccess.Should().BeTrue();

        foreach (var anchor in generated.Anchors)
            AccuracyCurveMath.EvaluateFraction(generated.Curve, anchor.X).Should().BeApproximately(anchor.Y, 1e-12);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public void FlatInteriorIntervals_ShouldStayFlatAfterOtherGenerations()
    {
        for (var iteration = 0; iteration < 2; iteration++)
        {
            NativeCurveGenerator.Generate([0.999, 0.9989, 0.7, 0.1], [0.95, 0.4, 0.01], 0.0001)
                .IsSuccess.Should().BeTrue();

            var generated = NativeCurveGenerator.Generate([0.99, 0.9, 0.8, 0.7], [0.6, 0.6, 0.6], 0.001).Value;

            for (var sample = 0; sample <= 100; sample++)
                AccuracyCurveMath.EvaluateFraction(generated.Curve, 0.7 + 0.2 * sample / 100)
                    .Should().BeApproximately(0.6, 1e-12);
        }
    }

    private static double[] ReferenceDerivatives(CurvePoint[] points)
    {
        var h = points.Zip(points.Skip(1), (a, b) => b.X - a.X).ToArray();
        var d = points.Zip(points.Skip(1), (a, b) => (b.Y - a.Y) / (b.X - a.X)).ToArray();
        var m = new double[points.Length];
        m[0] = End(h[0], h[1], d[0], d[1]);
        m[^1] = End(h[^1], h[^2], d[^1], d[^2]);
        for (var i = 1; i < points.Length - 1; i++)
        {
            if (d[i - 1] == 0 || d[i] == 0) continue;
            var weight = (2 * h[i] + h[i - 1]) / (3 * (h[i] + h[i - 1]));
            m[i] = 1 / (weight / d[i - 1] + (1 - weight) / d[i]);
        }
        return m;

        static double End(double a, double b, double p, double q)
            => Math.Clamp(((2 * a + b) * p - a * q) / (a + b), 0, 3 * p);
    }
}
