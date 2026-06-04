using GuildSaber.Api.Features.LegacyGS.Pipelines;

namespace GuildSaber.Api.Features.LegacyGS;

public static class LegacyGuildSaberInstaller
{
    public static IServiceCollection AddLegacyGuildSaberFeature(this IServiceCollection services) => services
        .AddTransient<LegacyGSImportAdminConfPipeline>()
        .AddTransient<LegacyGuildSaberMapImportPipeline>();
}