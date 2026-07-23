using System;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI.Components;

public class CategoryView : XUIHLayout
{
    private readonly Action<CategoryId> _callback;
    private readonly UIFactory _uiFactory;

    private CategoryId _categoryId = new(-1);
    private GSText _categoryNameText = null!;
    private GSSecondaryButton _downloadButton = null!;

    protected CategoryView(UIFactory uiFactory, Action<CategoryId> callback) : base("GuildSaberCategoryView")
    {
        _uiFactory = uiFactory;
        _callback = callback;

        OnReady(EventReady);
    }

    public static CategoryView Make(UIFactory uiFactory, Action<CategoryId> callback) => new(uiFactory, callback);

    private void EventReady(CHOrVLayout x)
    {
        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;

        _categoryNameText = _uiFactory.Text(string.Empty);
        _downloadButton = _uiFactory.SecondaryButton("Download");
        _downloadButton
            .OnClick(DownloadClicked)
            .SetWidth(15)
            .SetHeight(4);

        _downloadButton.BuildUI(x.transform);
        _categoryNameText.BuildUI(x.transform);
    }

    public void SetData(string categoryName, CategoryId categoryId)
    {
        _categoryId = categoryId;
        _categoryNameText.SetText(categoryName);
    }

    public void SetInteractable(bool interactable) => _downloadButton.SetInteractable(interactable);

    private void DownloadClicked()
    {
        _downloadButton.SetInteractable(false);
        _callback(_categoryId);
    }
}