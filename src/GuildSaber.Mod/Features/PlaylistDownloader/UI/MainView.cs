using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.PlaylistDownloader.UI.Components;
using SongCore;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderViewController : ViewController<PlaylistDownloaderViewController>
{
    private readonly List<CategoryView> _categoryViews = [];
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    [Inject] private readonly PlaylistDownloader _playlistDownloader = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;

    private XUIVLayout _categoriesListLayout = null!;
    private GSSecondaryButton _deleteAllButton = null!;
    private GSSecondaryButton _downloadAllButton = null!;
    private GSText _guildNameText = null!;
    private XUISlider _rangeDownloadMaxSlider = null!;
    private XUISlider _rangeDownloadMinSlider = null!;
    private XUIToggle _rangeDownloadToggle = null!;
    private GSText _uniquePlaylistDownloadedText = null!;

    public event Action OnResultsModalClosed = null!;

    protected override void OnViewCreation()
    {
        Templates.FullRectLayoutMainView(
                XUIHLayout.Make(
                    XUIVLayout.Make(
                        _uiFactory.SecondaryButton("Delete all")
                            .Bind(ref _deleteAllButton)
                            .SetWidth(20)
                            .SetHeight(6)
                            .OnClick(DeleteGuildsPlaylists)
                    ).SetBackground(true, new Color(0, 0, 0, 0.80f)),
                    XUIVLayout.Make(
                        _uiFactory.Text("Download or update [Insert guild name] playlists:")
                            .Bind(ref _guildNameText),
                        XUIHLayout.Make(
                            _uiFactory.Text("Range download:"),
                            XUIToggle.Make()
                                .OnValueChanged(RangeDownloadToggleChanged)
                                .Bind(ref _rangeDownloadToggle)
                        ),
                        XUIHLayout.Make(
                            _uiFactory.Text("Minimum level:"),
                            XUISlider.Make()
                                .OnValueChanged(RangeDownloadValueChanged)
                                .SetInteger(true)
                                .SetColor(Color.black)
                                .SetInteractable(false)
                                .Bind(ref _rangeDownloadMinSlider)
                        ),
                        XUIHLayout.Make(
                            _uiFactory.Text("Maximum level:"),
                            XUISlider.Make()
                                .OnValueChanged(RangeDownloadValueChanged)
                                .SetInteger(true)
                                .SetColor(Color.black)
                                .SetInteractable(false)
                                .Bind(ref _rangeDownloadMaxSlider)
                        ),
                        _uiFactory.SecondaryButton("Download all")
                            .Bind(ref _downloadAllButton)
                            .SetWidth(30)
                            .SetHeight(5)
                            .OnClick(DownloadAllClicked),
                        _uiFactory.Text(string.Empty)
                            .Bind(ref _uniquePlaylistDownloadedText)
                    ).SetBackground(true, new Color(0, 0, 0, 0.8f)),
                    XUIVLayout.Make()
                        .SetBackground(true, new Color(0, 0, 0, 0.8f))
                        .Bind(ref _categoriesListLayout)
                ))
            .SetSpacing(2)
            .OnReady(x => x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize)
            .BuildUI(transform);

        _playlistDownloader.EventUniquePlaylistDownloadCompleted += UniquePlaylistsDownloadFinished;
        _playlistDownloader.EventPlaylistsDownloadCompleted += DownloadFinished;
    }

    protected override void OnViewActivation()
    {
        if (_guildSaberManager.State is not GuildSaberRuntimeState.Ready(var snapshot)) return;

        var currentGuild = snapshot.CurrentGuildExtended;
        var guildName = currentGuild.Guild.Info.Name;
        var levels = snapshot.LevelStats;
        var categories = currentGuild.Categories;

        _guildNameText.SetText($"Download or update {guildName} playlists:");

        var filteredLevels = levels.Where(x => x.Level.Order != 100)
            .ToArray();

        float minLevel = filteredLevels.Length == 0 ? 0 : filteredLevels.Min(x => x.Level.Order);
        float maxLevel = filteredLevels.Length == 0 ? 0 : filteredLevels.Max(x => x.Level.Order);

        _rangeDownloadMinSlider
            .SetMinValue(minLevel)
            .SetMaxValue(maxLevel)
            .SetValue(minLevel);

        _rangeDownloadMaxSlider
            .SetMinValue(minLevel)
            .SetMaxValue(maxLevel)
            .SetValue(maxLevel);

        _categoryViews.ForEach(x => x.SetActive(false));

        for (var i = 0; i < categories.Length; i++)
        {
            if (i >= _categoryViews.Count)
            {
                var categoryView = CategoryView.Make(_uiFactory, DownloadCategoryClicked);
                categoryView.BuildUI(_categoriesListLayout.Element.transform);
                _categoryViews.Add(categoryView);
            }

            var category = categories[i];
            _categoryViews[i].SetData(category.Info.Name, category.Id);
            _categoryViews[i].SetActive(true);
        }
    }

    private void DeleteGuildsPlaylists()
    {
        if (_guildSaberManager.State is not GuildSaberRuntimeState.Ready(var snapshot))
        {
            ShowMessageModal("GuildSaber is not ready to delete playlists");
            return;
        }

        var guildPlaylistsPath = _playlistDownloader.GetGuildPlaylistsPath(snapshot.CurrentGuildExtended.Guild);
        if (!Directory.Exists(guildPlaylistsPath))
        {
            ShowMessageModal("Nothing to delete");
            return;
        }

        try
        {
            Directory.Delete(guildPlaylistsPath, true);
        }
        catch (Exception ex)
        {
            ShowMessageModal($"Failed playlists deletion:\n{ex.Message}");
            return;
        }

        ShowMessageModal("Successfully deleted playlists", TaskFinished);
    }

    private void RangeDownloadToggleChanged(bool value)
    {
        _rangeDownloadMinSlider.SetInteractable(value);
        _rangeDownloadMaxSlider.SetInteractable(value);
    }

    private void RangeDownloadValueChanged(float _)
    {
        var minValue = _rangeDownloadMinSlider.Element.GetValue();
        var maxValue = _rangeDownloadMaxSlider.Element.GetValue();

        if (minValue <= maxValue + Mathf.Epsilon) return;

        _rangeDownloadMinSlider.SetValue(maxValue, false);
        _rangeDownloadMaxSlider.SetValue(minValue, false);
    }

    private void UniquePlaylistsDownloadFinished(string category, string levelName, bool success)
        => _uniquePlaylistDownloadedText.SetText(success
            ? $"{category}: {levelName} successfully downloaded"
            : $"{category}: {levelName} failed");

    private void DownloadFinished(int successCount, int failCount)
    {
        var message = (successCount, failCount) switch
        {
            (successCount: 0, failCount: 0) => "There was nothing to download",
            (successCount: 0, _) => $"Failed to download {failCount} playlist{(failCount == 1 ? "" : "s")}",
            (_, failCount: 0) => $"Successfully downloaded {successCount} playlist{(successCount == 1 ? "" : "s")}",
            _ => $"Successfully downloaded {successCount} playlist{(successCount == 1 ? "" : "s")}\n" +
                 $"Failed to download {failCount} playlist{(failCount == 1 ? "" : "s")}"
        };

        _downloadAllButton.SetInteractable(true);
        _categoryViews.ForEach(x => x.SetInteractable(true));
        _deleteAllButton.SetInteractable(true);

        ShowMessageModal(message, TaskFinished);
    }

    private void DownloadAllClicked()
    {
        if (_rangeDownloadToggle.Element.GetValue())
            Download(_playlistDownloader.DownloadPlaylistsAsync(
                (int)_rangeDownloadMinSlider.Element.GetValue(),
                (int)_rangeDownloadMaxSlider.Element.GetValue(),
                null));
        else
            Download(_playlistDownloader.DownloadPlaylistsAsync());
    }

    private void DownloadCategoryClicked(CategoryId categoryId)
    {
        if (_rangeDownloadToggle.Element.GetValue())
            Download(_playlistDownloader.DownloadPlaylistsAsync(
                (int)_rangeDownloadMinSlider.Element.GetValue(),
                (int)_rangeDownloadMaxSlider.Element.GetValue(),
                categoryId));
        else
            Download(_playlistDownloader.DownloadPlaylistsAsync(categoryId));
    }

    private async void Download(Task task)
    {
        SetDownloadControls(false);
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            SetDownloadControls(true);
            ShowMessageModal($"Failed to download playlists:\n{exception.Message}");
        }
    }

    private void SetDownloadControls(bool interactable)
    {
        _downloadAllButton.SetInteractable(interactable);
        _categoryViews.ForEach(x => x.SetInteractable(interactable));
        _deleteAllButton.SetInteractable(interactable);
    }

    private void TaskFinished()
    {
        Loader.Instance.RefreshLevelPacks();
        Loader.Instance.RefreshSongs();

        OnResultsModalClosed.Invoke();
    }
}