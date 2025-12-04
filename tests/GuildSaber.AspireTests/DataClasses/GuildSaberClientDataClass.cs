using GuildSaber.CSharpClient;
using TUnit.Core.Interfaces;

namespace GuildSaber.AspireTests.DataClasses;

public class GuildSaberClientDataClass : IAsyncInitializer, IAsyncDisposable
{
    public HttpClient HttpClient { get; private set; } = null!;
    public GuildSaberClient GuildSaberClient { get; private set; } = null!;

    public async ValueTask DisposeAsync()
        => await Console.Out.WriteLineAsync("And when the class is finished with, we can clean up any resources.");

    public async Task InitializeAsync()
    {
        HttpClient = GlobalHooks.App!.CreateHttpClient("api");
        GuildSaberClient = new GuildSaberClient(HttpClient.BaseAddress!, HttpClient);
        if (GlobalHooks.NotificationService is null) return;

        await GlobalHooks.NotificationService
            .WaitForResourceAsync("api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
    }
}