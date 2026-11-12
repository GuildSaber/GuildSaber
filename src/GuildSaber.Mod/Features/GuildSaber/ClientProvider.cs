using System;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Helpers;

namespace GuildSaber.Mod.Features.GuildSaber;

public sealed class GuildSaberClientProvider(GuildSaberConfig config, Logger logger) : IDisposable
{
    private readonly object _clientLock = new();
    private (GuildSaberClient client, ApiEnv environment)? _clientData;

    public void Dispose()
    {
        lock (_clientLock)
        {
            _clientData?.client.Dispose();
            _clientData = null;
        }
    }

    public GuildSaberClient GetClient()
    {
        lock (_clientLock)
        {
            var environment = config.ApiEnv;
            if (_clientData?.environment == environment)
                return _clientData.Value.client;

            var apiUri = environment.ToApiUri();
            var cdnUri = environment.ToCdnUri();
            logger.Debug($"Creating GuildSaberClient for: {apiUri}, CDN: {cdnUri}");

            var client = new GuildSaberClient(apiUri, cdnUri, authentication: null);

            _clientData?.client.Dispose();
            _clientData = (client, environment);

            return _clientData.Value.client;
        }
    }
}