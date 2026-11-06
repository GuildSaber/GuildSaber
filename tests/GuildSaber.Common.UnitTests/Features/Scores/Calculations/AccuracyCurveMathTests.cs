using AwesomeAssertions;
using GuildSaber.Api.Features.Scores.Calculations;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.StrongTypes;
using JetBrains.Annotations;

namespace GuildSaber.Common.UnitTests.Features.Scores.Calculations;

[PublicAPI]
public class AccuracyCurveMathTests
{
    [Test]
    public void AscendingSamples_ShouldCanonicalizeAndInterpolateIrregularIntervals()
    {
        CurvePoint[] input = [(0, 0), (0.2, 0.3), (0.85, 0.6), (1, 1)];
        var curve = AccuracyCurveMath.Validate(input).Value;
        curve.Points.Should().Equal(input.Reverse());
        foreach (var knot in input)
            AccuracyCurveMath.EvaluateFraction(curve, knot.X).Should().Be(knot.Y);
        AccuracyCurveMath.EvaluateFraction(curve, 0.525).Should().BeApproximately(0.45, 1e-14);
        AccuracyCurveMath.EvaluateFraction(curve, 0.925).Should().BeApproximately(0.8, 1e-14);
        input[0].X.Should().Be(0, "validation must not mutate caller-owned data");
    }

    [Test]
    public void InvalidSamples_ShouldFailInsteadOfClampingOrSortingAmbiguousInput()
    {
        CurvePoint[][] invalid =
        [
            [], [(0, 0)], [(0.1, 0), (1, 1)], [(0, 0), (0.99, 1)],
            [(0, 0), (0.5, 0.4), (0.5, 0.8), (1, 1)],
            [(0, 0), (0.8, 0.4), (0.3, 0.8), (1, 1)],
            [(0, 0), (double.NaN, 0.4), (1, 1)], [(0, 0), (1, double.PositiveInfinity)],
            [(0, 0), (1, 1.01)], [(-0.1, 0), (1, 1)]
        ];
        foreach (var samples in invalid)
            AccuracyCurveMath.Validate(samples).IsFailure.Should().BeTrue();
        AccuracyCurveMath.Validate(null).IsFailure.Should().BeTrue();
        AccuracyCurveMath.Validate(new CurvePoint[257]).IsFailure.Should().BeTrue();
    }

    [Test]
    public void EditableNonmonotoneShape_ShouldRemainExactButFailNativeFallbackValidation()
    {
        CurvePoint[] samples = [(0, 0.2), (0.5, 1), (0.8, 0.1), (1, 0.5)];
        var curve = AccuracyCurveMath.Validate(samples).Value;
        AccuracyCurveMath.EvaluateFraction(curve, 0.5).Should().Be(1);
        AccuracyCurveMath.EvaluateFraction(curve, 0.8).Should().Be(0.1);
        AccuracyCurveMath.Validate(samples, requireMonotone: true).IsFailure.Should().BeTrue();
    }

    [Test]
    public void BonusComposition_ShouldRetainBaseAndScaleExactlyOnce()
    {
        var curve = AccuracyCurveMath.Validate([(0, 0), (0.9, 0.6), (1, 1)]).Value;
        var accuracy = Accuracy.TryCreate(90).Value;
        ((float)AccuracyCurveMath.Calculate(100, 0.25, accuracy, curve).Value).Should().Be(115);
        ((float)AccuracyCurveMath.Calculate(100, 0.25, Accuracy.TryCreate(100).Value, curve).Value).Should().Be(125);
        ((float)AccuracyCurveMath.Calculate(200, 0.25, accuracy, curve).Value).Should().Be(230);
        ((float)AccuracyCurveMath.Calculate(100, 0.5, accuracy, curve).Value).Should().Be(130);
        ((float)AccuracyCurveMath.Calculate(100, 2, accuracy, curve).Value).Should().Be(220);
        ((float)AccuracyCurveMath.Calculate(100, 0, accuracy, null).Value).Should().Be(100);
        ((float)AccuracyCurveMath.Calculate(0, 0.25, accuracy, null).Value).Should().Be(0);
    }

    [Test]
    public void UnusableAccuracy_ShouldKeepBaseRewardWithoutAnAccuracyBonus()
    {
        var point = new Point
        {
            CurveSettings = new CurveSettings
            {
                Difficulty = new CustomCurve([(32, 100), (0, 100)]),
                Accuracy = new CustomCurve([(1, 1), (0, 0)]),
                AccuracyBonusRatio = 0.25
            }
        };
        var rating = new RankedMapRating { AccStar = default, DiffStar = new RankedMapRating.DifficultyStar(20) };
        var score = BaseScore.TryCreate(1200).Value;

        ((float)ScoringUtils.CalculateRawPoints(score, default, MaxScore.TryCreate(1000).Value, point, rating))
            .Should().Be(100);
        ((float)ScoringUtils.CalculateRawPoints(score, default, default, point, rating)).Should().Be(100);
    }

    [Test]
    public void CloseScores_ShouldRetainDistinctNativeRewardsThroughTheStrongType()
    {
        var maximum = MaxScore.TryCreate(20_000_000).Value;
        var lower = Accuracy.From(BaseScore.TryCreate(19_000_000).Value, maximum).Value;
        var higher = Accuracy.From(BaseScore.TryCreate(19_000_001).Value, maximum).Value;
        var curve = NativeCurveGenerator.Generate([higher / 100d, lower / 100d], [0.8], 0.001).Value.Curve;

        AccuracyCurveMath.Evaluate(curve, higher).Should().Be(1);
        AccuracyCurveMath.Evaluate(curve, lower).Should().Be(0.8);
        ((float)AccuracyCurveMath.Calculate(100, 0.25, higher, curve).Value).Should().Be(125);
        ((float)AccuracyCurveMath.Calculate(100, 0.25, lower, curve).Value).Should().Be(120);
    }

    [Test]
    public void UnrepresentableRewards_ShouldFail()
    {
        var accuracy = Accuracy.TryCreate(90).Value;
        AccuracyCurveMath.Calculate(float.MaxValue, 0.25, accuracy, null).IsFailure.Should().BeTrue();
        AccuracyCurveMath.Calculate(100, double.PositiveInfinity, accuracy, null).IsFailure.Should().BeTrue();
        AccuracyCurveMath.Calculate(100, -0.1, accuracy, null).IsFailure.Should().BeTrue();
    }
}