using System.Text.Json;
using System.Text.Json.Serialization;
using TUnit.Core.Interfaces;

namespace GuildSaber.AspireTests.Data;

public class HttpClientDataClass : IAsyncInitializer, IAsyncDisposable
{
    public HttpClient HttpClient { get; private set; } = new();

    public JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async ValueTask DisposeAsync()
        => await Console.Out.WriteLineAsync("And when the class is finished with, we can clean up any resources.");

    public async Task InitializeAsync()
    {
        HttpClient = GlobalHooks.App!.CreateHttpClient("api");
        if (GlobalHooks.NotificationService is null) return;

        await GlobalHooks.NotificationService
            .WaitForResourceAsync("api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
    }
}