namespace GuildSaber.Api.Features.PointCurves.Http;

public static class PointCurveResponses
{
    public sealed record Settings(
        PointCurveRequests.Sample[] DifficultyCurve,
        PointCurveRequests.Sample[] DefaultAccuracyCurve,
        double AccuracyBonusRatio,
        PointCurveRequests.Policy Policy
    );

    public sealed record Curve(
        PointCurveRequests.Sample[] Points,
        bool IsMapOverride,
        bool Editable,
        DateTimeOffset? UpdatedAt,
        double[] BenchmarkAccuracies
    );

    public sealed record Preview(
        Curve Curve,
        double BasePoints,
        double AccuracyBonusRatio,
        double MaximumAdditionalPoints,
        PreviewValue[] Values
    );

    public sealed record PreviewValue(double Accuracy, double BonusFraction, float RawPoints);
}