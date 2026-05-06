using Zenject;

namespace GuildSaber.Mod.Features.GuildSaber.Settings;

public class GuildSaberSettingsInstaller : Installer
{
    public override void InstallBindings() => Container
        .Bind<GuildSaberSettingsView>()
        .FromFactory<GuildSaberSettingsFactory>()
        .AsSingle()
        .NonLazy();
}