using Zenject;

namespace GuildSaber.Mod.Features.Common.UI;

public class UIInstaller : Installer
{
    public override void InstallBindings() => Container
        .Bind<UIFactory>()
        .AsSingle();
}