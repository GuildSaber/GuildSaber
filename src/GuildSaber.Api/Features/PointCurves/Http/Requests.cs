using System.Text.Json.Serialization;

namespace GuildSaber.Api.Features.PointCurves.Http;

public static class PointCurveRequests
{
    public readonly record struct Sample([property: JsonRequired] double X, [property: JsonRequired] double Y);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(Shared), "Shared")]
    [JsonDerivedType(typeof(EditableMapCurve), "EditableMapCurve")]
    [JsonDerivedType(typeof(ScoreDerivedMapCurve), "ScoreDerivedMapCurve")]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public abstract record Policy
    {
        public sealed record Shared : Policy;
        public sealed record EditableMapCurve : Policy;
        public sealed record ScoreDerivedMapCurve([property: JsonRequired] double[] RunnerUpRewardFractions) : Policy;
    }

    public sealed record UpdateSettings(
        [property: JsonRequired] Sample[] DifficultyCurve,
        [property: JsonRequired] Sample[] DefaultAccuracyCurve,
        [property: JsonRequired] double AccuracyBonusRatio,
        [property: JsonRequired] Policy Policy);

    public sealed record PutCurve([property: JsonRequired] Sample[] Points);
    public sealed record Preview(Sample[]? Points = null, double[]? Accuracies = null);
}