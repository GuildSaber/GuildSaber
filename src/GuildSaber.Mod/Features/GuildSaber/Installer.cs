using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Helpers;
using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber;

internal class GuildSaberClientFactory(GuildSaberConfig config, Logger logger) : IFactory<GuildSaberClient>
{
    public GuildSaberClient Create()
    {
        var apiUri = config.ApiEnv.ToApiUri();
        var cdnUri = config.ApiEnv.ToCdnUri();
        logger.Debug($"Creating GuildSaberClient for: {apiUri}, CDN: {cdnUri}");

        return new GuildSaberClient(apiUri, cdnUri, authentication: null);
    }
}

public class GuildSaberInstaller(GuildSaberConfig config) : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GuildSaberClient>()
            .FromFactory<GuildSaberClient, GuildSaberClientFactory>()
            .AsSingle();

        Container.Bind<GuildSaberSession>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildAssetCache>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberCacheStore>().AsSingle();

        Container.BindInstance(config);
    }
}