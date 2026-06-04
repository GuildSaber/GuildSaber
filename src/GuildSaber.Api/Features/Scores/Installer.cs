using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Features.Scores.Workers;

namespace GuildSaber.Api.Features.Scores;

public static class ScoresInstaller
{
    public static IServiceCollection AddScoresFeature(this IServiceCollection services) => services
        .AddTransient<ScoreAddOrUpdatePipeline>()
        .AddHostedService<BeatLeaderScoreSyncWorker>();
}