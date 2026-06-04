using GuildSaber.Api.Queuing;

namespace GuildSaber.Api.Setup;

public static class BackgroundQueuesSetup
{
    public static WebApplicationBuilder AddBackgroundQueues(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<QueueProcessingService>();
        builder.Services.AddHostedService<HeavyQueueProcessingService>();
        builder.Services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));
        builder.Services.AddSingleton<IHeavyBackgroundTaskQueue>(_ => new HeavyBackgroundTaskQueue(capacity: 10));

        return builder;
    }
}