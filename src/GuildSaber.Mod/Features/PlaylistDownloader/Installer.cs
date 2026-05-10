using GuildSaber.Mod.Features.PlaylistDownloader.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader;

public class PlaylistDownloaderInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<PlaylistDownloader>().AsSingle();
        Container.Bind<PlaylistDownloaderViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<PlaylistDownloaderCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
    }
}