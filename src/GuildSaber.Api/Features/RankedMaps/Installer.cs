using GuildSaber.Api.Features.RankedMaps.Pipelines;

namespace GuildSaber.Api.Features.RankedMaps;

public static class RankedMapsInstaller
{
    public static IServiceCollection AddRankedMapsFeature(this IServiceCollection services) => services
        .AddScoped<RankedMapService>()
        .AddTransient<AddRankedMapPipeline>()
        .AddTransient<EditRankedMapPipeline>();
}