using System;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI.Components;

public class CategoryView : XUIHLayout
{
    private readonly Action<CategoryId> _callback;

    private CategoryId _categoryId = new(-1);
    private XUIText _categoryNameText = null!;
    private XUISecondaryButton _downloadButton = null!;

    protected CategoryView(Action<CategoryId> callback) : base("GuildSaberCategoryView")
    {
        _callback = callback;

        OnReady(EventReady);
    }

    public static CategoryView Make(Action<CategoryId> callback) => new(callback);

    private void EventReady(CHOrVLayout x)
    {
        x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleLeft;

        _categoryNameText = XUIText.Make(string.Empty);
        _downloadButton = XUISecondaryButton.Make("Download");
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