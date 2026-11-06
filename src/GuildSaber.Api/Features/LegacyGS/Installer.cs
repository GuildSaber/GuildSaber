using GuildSaber.Api.Features.LegacyGS.Jobs;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Api.Scheduling;

namespace GuildSaber.Api.Features.LegacyGS;

public static class LegacyGuildSaberInstaller
{
    public static IServiceCollection AddLegacyGuildSaberFeature(this IServiceCollection services) => services
        .AddTransient<LegacyGSImportAdminConfPipeline>()
        .AddTransient<LegacyGuildSaberMapImportPipeline>()
        .AddCronJob<ImportLegacyGuildSaberMapsJob>(
            "0 0 5 * * *", static (job, token) => job.DoWorkAsync(token))
        .AddCronJob<ImportLegacyGuildSaberAdminConfirmationsJob>(
            "0 0 10 * * *", static (job, token) => job.DoWorkAsync(token));
}