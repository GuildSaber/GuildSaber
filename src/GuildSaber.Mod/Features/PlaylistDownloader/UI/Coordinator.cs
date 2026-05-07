using GuildSaber.Mod.Features.Common.UI.Components;
using HMUI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderCoordinator : CustomFlowCoordinator
{
    [Inject] private readonly PlaylistDownloaderViewController _playlistDownloaderViewController = null!; 
    
    protected override string Title => "Guild Saber playlist downloader";

    protected override ViewController? GetMainViewController() => _playlistDownloaderViewController;

    protected override void OnCreation()
    {
        _playlistDownloaderViewController.EventClosedResultsModal += Dismiss;
    }
}