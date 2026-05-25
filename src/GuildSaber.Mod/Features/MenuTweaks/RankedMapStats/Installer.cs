using Zenject;

namespace GuildSaber.Mod.Features.MenuTweaks.RankedMapStats;

public class MapRankedStatsInstaller : Installer
{
    public override void InstallBindings() => Container
        .BindInterfacesAndSelfTo<RankedMapStats>()
        .AsSingle();
}