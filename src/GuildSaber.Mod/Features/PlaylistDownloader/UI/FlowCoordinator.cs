using GuildSaber.Mod.Features.Common.UI;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderCoordinator : SimpleFlowCoordinator
{
    [Inject] private readonly PlaylistDownloaderViewController _playlistDownloaderViewController = null!;

    protected override string Title => "Guild Saber playlist downloader";
    protected override ViewController GetMainViewController() => _playlistDownloaderViewController;

    protected override void OnCreation() => _playlistDownloaderViewController.OnResultsModalClosed += Dismiss;
}