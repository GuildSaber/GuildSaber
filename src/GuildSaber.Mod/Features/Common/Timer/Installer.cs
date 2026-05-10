using Zenject;

namespace GuildSaber.Mod.Features.Common.Timer;

public class TimerInstaller : Installer
{
    public override void InstallBindings() => Container
        .Bind<Timer>()
        .FromNewComponentOnNewGameObject()
        .AsSingle();
}