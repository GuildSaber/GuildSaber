using System.Linq;
using Zenject;

namespace GuildSaber.Mod.Features.RankedMapStats;

public class MapRankedStatsInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<GameplayModifiersPanelController>().FromMethod(x => UnityEngine.Resources.FindObjectsOfTypeAll<GameplayModifiersPanelController>().First()).AsCached();
        Container.Bind<RankedMapStats>().AsSingle().NonLazy();
    }
}