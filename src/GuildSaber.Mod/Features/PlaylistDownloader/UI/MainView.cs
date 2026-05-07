using System;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderViewController : ViewController<PlaylistDownloaderViewController>
{
    [Inject] private readonly PlaylistDownloader _playlistDownloader = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;
    [Inject] private readonly GuildSaberCache _cache = null!;
    [Inject] private readonly GuildSaberConfig _config = null!;
     
    public event Action EventClosedResultsModal = null!;

    private GSText _uniquePlaylistDownloadedText = null!;
    
    protected override void OnViewCreation()
    {
        string guildName = _cache.GuildsExtended[_config.PlayerCard.GuildId].Guild.Info.Name;
            
        Templates.FullRectLayoutMainView(
            
            _uiFactory.Text($"Download or update {guildName} playlists:"),
            _uiFactory.SecondaryButton("Download")
                .SetWidth(30)
                .SetHeight(5)
                .OnClick(DownloadClicked),
            _uiFactory.Text(string.Empty)
                .Bind(ref _uniquePlaylistDownloadedText)
            ).BuildUI(transform);

        _playlistDownloader.EventUniquePlaylistDownloadCompleted += UniquePlaylistsDownloadFinished;
        _playlistDownloader.EventPlaylistsDownloadCompleted += DownloadFinished;
    }

    private void UniquePlaylistsDownloadFinished(string category, string levelName, bool success)
    {
        if (success)
        {
            _uniquePlaylistDownloadedText.SetText($"{category}: {levelName} successfully downloaded");
        }
        else
        {
            _uniquePlaylistDownloadedText.SetText($"{category}: {levelName} failed");
        }
    }

    private void DownloadFinished(int successCount, int failCount)
    {
        string message = string.Empty;

        if (successCount == 0 && failCount == 0)
        {
            message = "There was nothing to download";
        }
        else
        {
            if (successCount != 0)
            {
                message += $"Successfully downloaded {successCount} playlist{(successCount == 1 ? "" : "s")}\n";
            }

            if (failCount != 0)
            {
                message += $"Failed to download {failCount} playlist{(failCount == 1 ? "" : "s")}";
            }
        }
        
        ShowMessageModal(message, DownloadFinished);
    }
    
    private void DownloadClicked()
    {
        _playlistDownloader.DownloadPlaylists();
    }

    private void DownloadFinished()
    {
        SongCore.Loader.Instance.RefreshSongs();
        
        EventClosedResultsModal.Invoke();
    }
}