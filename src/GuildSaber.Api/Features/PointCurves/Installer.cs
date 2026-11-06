namespace GuildSaber.Api.Features.PointCurves;

public static class PointCurvesInstaller
{
    public static IServiceCollection AddPointCurvesFeature(this IServiceCollection services) => services
        .AddScoped<PointCurveService>()
        .AddTransient<MapPointCurvePipeline>();
}
