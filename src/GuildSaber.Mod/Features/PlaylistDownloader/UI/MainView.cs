using System;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using SongCore;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderViewController : ViewController<PlaylistDownloaderViewController>
{
    [Inject] private readonly GuildSaberCache _cache = null!;
    [Inject] private readonly GuildSaberConfig _config = null!;
    [Inject] private readonly PlaylistDownloader _playlistDownloader = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;
    private GSSecondaryButton _downloadButton = null!;

    private GSText _uniquePlaylistDownloadedText = null!;

    public event Action OnResultsModalClosed = null!;

    protected override void OnViewCreation()
    {
        var guildName = _cache.GuildsExtended[_config.GuildId].Guild.Info.Name;

        Templates.FullRectLayoutMainView(
                _uiFactory.Text($"Download or update {guildName} playlists:"),
                _uiFactory.SecondaryButton("Download")
                    .Bind(ref _downloadButton)
                    .SetWidth(30)
                    .SetHeight(5)
                    .OnClick(DownloadClicked),
                _uiFactory.Text(string.Empty)
                    .Bind(ref _uniquePlaylistDownloadedText)
            )
            .SetSpacing(2)
            .OnReady(x => x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize)
            .BuildUI(transform);

        _playlistDownloader.EventUniquePlaylistDownloadCompleted += UniquePlaylistsDownloadFinished;
        _playlistDownloader.EventPlaylistsDownloadCompleted += DownloadFinished;
    }

    private void UniquePlaylistsDownloadFinished(string category, string levelName, bool success)
        => _uniquePlaylistDownloadedText.SetText(success
            ? $"{category}: {levelName} successfully downloaded"
            : $"{category}: {levelName} failed");

    private void DownloadFinished(int successCount, int failCount)
    {
        var message = string.Empty;

        if (successCount == 0 && failCount == 0)
        {
            message = "There was nothing to download";
        }
        else
        {
            if (successCount != 0)
                message += $"Successfully downloaded {successCount} playlist{(successCount == 1 ? "" : "s")}\n";

            if (failCount != 0)
                message += $"Failed to download {failCount} playlist{(failCount == 1 ? "" : "s")}";
        }

        _downloadButton.SetInteractable(true);
        ShowMessageModal(message, DownloadFinished);
    }

    private void DownloadClicked()
    {
        _playlistDownloader.DownloadPlaylists();
        _downloadButton.SetInteractable(false);
    }

    private void DownloadFinished()
    {
        Loader.Instance.RefreshSongs();
        OnResultsModalClosed.Invoke();
    }
}