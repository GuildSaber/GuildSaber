using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.PlaylistDownloader.UI.Components;
using SongCore;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI;

public class PlaylistDownloaderViewController : ViewController<PlaylistDownloaderViewController>
{
    [Inject] private readonly GuildSaberCache _cache = null!;
    private readonly List<CategoryView> _categoryViews = [];
    [Inject] private readonly GuildSaberConfig _config = null!;
    [Inject] private readonly PlaylistDownloader _playlistDownloader = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;

    private XUIVLayout _categoriesListLayout = null!;

    private GSSecondaryButton _downloadButton = null!;

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
                            .Bind(ref _downloadButton)
                            .SetWidth(30)
                            .SetHeight(5)
                            .OnClick(DownloadClicked),
                        _uiFactory.Text(string.Empty)
                            .Bind(ref _uniquePlaylistDownloadedText)
                    )
                    .SetBackground(true, new Color(0, 0, 0, 0.8f)),
                    XUIVLayout.Make(
                    )
                    .SetBackground(true, new Color(0, 0, 0, 0.8f))
                    .Bind(ref _categoriesListLayout)
                )
            )
            .SetSpacing(2)
            .OnReady(x => x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize)
            .BuildUI(transform);

        _playlistDownloader.EventUniquePlaylistDownloadCompleted += UniquePlaylistsDownloadFinished;
        _playlistDownloader.EventPlaylistsDownloadCompleted += DownloadFinished;
    }

    protected override void OnViewActivation()
    {
        var guildName = _cache.GuildsExtended[_config.GuildId].Guild.Info.Name;
        var levels = _cache.MemberLevelStats[_config.ContextId];
        var categories = _cache.GuildsExtended[_config.GuildId].Categories;

        _guildNameText.SetText($"Download or update {guildName} playlists:");


        var filteredLevels = levels.Where(x => x.Level.Order != 100)
            .ToArray();

        float minLevel = filteredLevels.Min(x => x.Level.Order);
        float maxLevel = filteredLevels.Max(x => x.Level.Order);

        _rangeDownloadMinSlider.SetMinValue(minLevel);
        _rangeDownloadMinSlider.SetMaxValue(maxLevel);
        _rangeDownloadMaxSlider.SetMinValue(minLevel);
        _rangeDownloadMaxSlider.SetMaxValue(maxLevel);

        _categoryViews.ForEach(x => x.SetActive(false));

        for (var x = 0; x < categories.Length; x++)
        {
            if (x >= _categoryViews.Count)
            {
                var categoryView = CategoryView.Make(_uiFactory, CategoryDownloadPressed);
                categoryView.BuildUI(_categoriesListLayout.Element.transform);
                _categoryViews.Add(categoryView);
            }

            var category = categories[x];
            _categoryViews[x].SetData(category.Info.Name, category.Id);
            _categoryViews[x].SetActive(true);
        }
    }

    private void DeleteGuildsPlaylists()
    {
        var guildName = _cache.GuildsExtended[_config.GuildId].Guild.Info.Name;
        var dirName = $"./Playlists/GuildSaber/{guildName}";
            
        if (!Directory.Exists(dirName))
        {
            ShowMessageModal("Nothing to delete");
        }

        try
        {
            var dirs = Directory.EnumerateDirectories(dirName);

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                
                var files = Directory.EnumerateFiles($"{dir}");
                
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessageModal($"Failed playlists deletion:\n{ex.Message}");
            return;
        }
        
        ShowMessageModal("Successfully deleted playlists", TaskFinished);
    }
    
    private void CategoryDownloadPressed(CategoryId categoryId)
    {
        if (_rangeDownloadToggle.Element.GetValue())
            _playlistDownloader.DownloadPlaylists(
                (int)_rangeDownloadMinSlider.Element.GetValue(),
                (int)_rangeDownloadMaxSlider.Element.GetValue(),
                categoryId);
        else
            _playlistDownloader.DownloadPlaylists(categoryId);
    }

    private void RangeDownloadToggleChanged(bool value)
    {
        _rangeDownloadMinSlider.SetInteractable(value);
        _rangeDownloadMaxSlider.SetInteractable(value);
    }

    private void RangeDownloadValueChanged(float _)
    {
        if (_rangeDownloadMinSlider.Element.GetValue() > _rangeDownloadMaxSlider.Element.GetValue())
            _rangeDownloadMinSlider.SetValue(_rangeDownloadMaxSlider.Element.GetValue(), false);
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
        foreach (var categoryView in _categoryViews)
            categoryView.OnFinished();

        ShowMessageModal(message, TaskFinished);
    }

    private void DownloadClicked()
    {
        _downloadButton.SetInteractable(false);

        if (_rangeDownloadToggle.Element.GetValue())
            _playlistDownloader.DownloadPlaylists(
                (int)_rangeDownloadMinSlider.Element.GetValue(),
                (int)_rangeDownloadMaxSlider.Element.GetValue(),
                null);
        else
            _playlistDownloader.DownloadPlaylists();
    }

    private void TaskFinished()
    {
        Loader.Instance.RefreshSongs();
        OnResultsModalClosed.Invoke();
    }
}