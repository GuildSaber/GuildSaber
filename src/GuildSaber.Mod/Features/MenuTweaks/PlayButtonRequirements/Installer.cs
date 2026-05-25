using System.Linq;
using Zenject;

namespace GuildSaber.Mod.Features.MenuTweaks.PlayButtonRequirements;

public sealed class PlayButtonRequirementsInstaller : Installer<PlayButtonRequirementsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<GameplayModifiersPanelController>()
            // Use FindObjectsOfTypaAll().First() because FromComponentInHierarchy() doesn't take the First one. (There is another one for multiplayer)
            .FromMethod(_ => UnityEngine.Resources.FindObjectsOfTypeAll<GameplayModifiersPanelController>().First())
            .AsCached();

        Container.BindInterfacesAndSelfTo<PlayButtonRequirements>()
            .AsSingle();
    }
}