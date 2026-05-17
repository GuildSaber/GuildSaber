using System.Linq;
using CP_SDK.UI.Modals;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI;

public class UIInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<LoadingControl>()
            .WithId(Constants.LoadingControlTemplateId)
            .FromMethod(x => UnityEngine.Resources.FindObjectsOfTypeAll<LoadingControl>().First()).AsCached();
        Container.Bind<UIFactory>().AsSingle();
    }
}