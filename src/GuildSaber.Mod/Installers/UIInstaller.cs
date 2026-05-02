using System.Linq;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.RankedMap;
using IPA.Utilities;
using Zenject;

namespace GuildSaber.Mod.Installers;

public class UIInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<UIFactory>().AsSingle();
    }
}