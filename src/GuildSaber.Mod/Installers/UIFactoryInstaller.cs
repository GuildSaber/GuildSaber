using GuildSaber.Mod.Core.UI;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class UIFactoryInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<UIFactory>().AsSingle();
    }
}