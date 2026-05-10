using GuildSaber.CSharpClient;
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
        Container.Bind<GuildSaberClient>().FromFactory<GuildSaberClientFactory>().AsSingle();
        Container.Bind<GuildSaberCache>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();

        Container.BindInstance(config);
    }
}