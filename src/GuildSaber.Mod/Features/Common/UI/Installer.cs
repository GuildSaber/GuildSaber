using System.Linq;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI;

public class UIInstaller : Installer
{
    public override void InstallBindings() => Container.Bind<LoadingControl>()
        .WithId(Constants.LoadingControlTemplateId)
        .FromMethod(_ => UnityEngine.Resources.FindObjectsOfTypeAll<LoadingControl>().First())
        .AsTransient();
}