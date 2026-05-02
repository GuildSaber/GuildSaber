using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Extensions;
using SiraUtil.Logging;
using Zenject;

namespace GuildSaber.Mod.Installers;

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

public class CoreInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<GuildSaberClient>().FromFactory<GuildSaberClientFactory>().AsSingle();
        Container.Bind<ModData>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();
        
    }
}