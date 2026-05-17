using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<GuildSaberSettingsView>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<GuildSaberSettingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        Container.Bind<GuildSaberMenuButton>().WithId(Constants.MenuButtonId).AsSingle().NonLazy();
    }
}