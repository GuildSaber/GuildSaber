using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Redis;
using Projects;

[assembly: Retry(3)]
[assembly: ExcludeFromCodeCoverage]

namespace GuildSaber.AspireTests;

public class GlobalHooks
{
    public static DistributedApplication? App { get; private set; }
    public static ResourceNotificationService? NotificationService { get; private set; }

    [Before(TestSession)]
    public static async Task SetUp()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<GuildSaber_AppHost>(
            args: [],
            configureBuilder: (appOptions, _) => appOptions.AllowUnsecuredTransport = true);

        appHost.WithContainersLifetime(ContainerLifetime.Session);
        appHost.WithRandomVolumeNames();
        appHost.RemoveResources<PgWebContainerResource>();
        appHost.RemoveResources<RedisCommanderResource>();

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await appHost.BuildAsync();
        NotificationService = App.Services.GetRequiredService<ResourceNotificationService>();

        await App.StartAsync();
    }

    [After(TestSession)]
    public static void CleanUp()
        => App?.Dispose();
}