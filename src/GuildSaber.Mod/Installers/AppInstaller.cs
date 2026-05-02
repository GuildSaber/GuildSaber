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
        Container.Bind<PlayerCardSettingsMainView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectionViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectionFlowCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<GuildSelectionFlowCoordinator>).AsSingle();
        Container.Bind<PlayerCardSettingsCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<PlayerCardSettingsCoordinator>).AsSingle();
    }
}

