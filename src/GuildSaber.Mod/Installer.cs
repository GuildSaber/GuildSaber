using BeatSaberMarkupLanguage;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;
using GuildSaber.Mod.Features.PlayerCard.UI.Settings;
using Zenject;

namespace GuildSaber.Mod;

internal class AppInstaller(GuildSaberConfig config) : Installer
{
    public override void InstallBindings()
    {
        Container.BindInstance(config);
        Container.Bind<PlayerCardSettingsMainView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectorViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSelectorFlowCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<GuildSelectorFlowCoordinator>).AsSingle();
        Container.Bind<PlayerCardSettingsCoordinator>()
            .FromMethod(BeatSaberUI.CreateFlowCoordinator<PlayerCardSettingsCoordinator>).AsSingle();
    }
}