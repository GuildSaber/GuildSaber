using Zenject;

namespace GuildSaber.Mod.Features.RankedMapStats;

public class MapRankedStatsInstaller : Installer
{
    public override void InstallBindings() => Container
        .Bind<RankedMapStats>()
        .AsSingle()
        .NonLazy();
}