using GuildSaber.Api.Features.RankedScores.Pipelines;

namespace GuildSaber.Api.Features.RankedScores;

public static class RankedScoresInstaller
{
    public static IServiceCollection AddRankedScoresFeature(this IServiceCollection services) => services
        .AddScoped<RankedScoreService>()
        .AddTransient<RankedScoreConfirmationPipeline>();
}