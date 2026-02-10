using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Extensions;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Installers;

internal class AppInstaller(PluginConfig config) : Installer
{
    public override void InstallBindings()
    {
        Container.BindInstance(config);
        Container.Bind<GuildSaberClient>().FromFactory<GuildSaberClientFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();
    }
}

internal class GuildSaberClientFactory(PluginConfig config, SiraLog logger) : IFactory<GuildSaberClient>
{
    public GuildSaberClient Create()
    {
        var apiUri = config.ApiEnv.ToApiUri;
        var cdnUri = config.ApiEnv.ToCdnUri;
        logger.Debug($"Creating GuildSaberClient for: {apiUri}, CDN: {cdnUri}");

        return new GuildSaberClient(apiUri, cdnUri, authentication: null);
    }
}