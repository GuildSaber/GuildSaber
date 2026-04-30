using BeatSaberMarkupLanguage;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.PlayerCard.UI.Settings;
using GuildSaber.Mod.Core.UI.Guild;
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
        Container.Bind<ModData>().AsSingle();
        Container.BindInterfacesAndSelfTo<GuildSaberManager>().AsSingle();
        Container.Bind<PlayerCardSettingsMainView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectionViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectionFlowCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<GuildSelectionFlowCoordinator>).AsSingle();
        Container.Bind<PlayerCardSettingsCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<PlayerCardSettingsCoordinator>).AsSingle();
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