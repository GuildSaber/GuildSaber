using Zenject;

namespace GuildSaber.Mod.Features.RankedMap;

public sealed class RankedMapInstaller : Installer<RankedMapInstaller>
{
    public override void InstallBindings() => Container
        .BindInterfacesAndSelfTo<RankedMapManager>()
        .AsSingle();
}