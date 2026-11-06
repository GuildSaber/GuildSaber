namespace GuildSaber.Api.Features.PointCurves;

public closed record PointCurveError
{
    public sealed record NotFound : PointCurveError;
    public sealed record ReadOnly(string Message) : PointCurveError;
    public sealed record Validation(string Field, string Message) : PointCurveError;
}
