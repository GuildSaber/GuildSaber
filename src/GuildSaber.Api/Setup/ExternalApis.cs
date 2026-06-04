using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Common.Services.ScoreSaber;

namespace GuildSaber.Api.Setup;

public static class ExternalApisSetup
{
    public static WebApplicationBuilder AddExternalApis(this WebApplicationBuilder builder)
    {
        // Configured pooled connection lifetime to avoid DNS issues in long-running services.
#pragma warning disable EXTEXP0001
        builder.Services.AddHttpClient<BeatLeaderApi>(client =>
            {
                client.BaseAddress = new Uri("https+http://beatleader-api");
                client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
            }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
            // Remove the resilience handlers because some the timeout policies interfere with endpoints returning 500s on purpose.
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        builder.Services.AddHttpClient<ScoreSaberApi>(client =>
            {
                client.BaseAddress = new Uri("https+http://scoresaber-api");
                client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
            }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        builder.Services.AddHttpClient<BeatSaverApi>(client =>
            {
                client.BaseAddress = new Uri("https+http://beatsaver-api");
                client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
            }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        builder.Services
            .AddHttpClient<LegacyGuildSaberApi>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
            })
            .UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        // Since they hold state, they should be transient.
        builder.Services.AddTransient<BeatLeaderGeneralSocketStream>(_ =>
        {
            var uri = builder.Configuration.GetValue<Uri>("services:beatleader-socket:default:0")
                      ?? new Uri("wss://sockets.api.beatleader.com/");
            return new BeatLeaderGeneralSocketStream(uri);
        });

        return builder;
    }
}