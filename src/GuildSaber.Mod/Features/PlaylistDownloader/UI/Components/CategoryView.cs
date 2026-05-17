using System;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;

namespace GuildSaber.Mod.Features.PlaylistDownloader.UI.Components;

public class CategoryView : XUIHLayout
{
    private UIFactory _uiFactory = null!;
    private GSText _categoryNameText = null!;
    private GSSecondaryButton _downloadButton = null!;

    private CategoryId _categoryId = new CategoryId(-1);

    private readonly Action<CategoryId> _callback;
    
    protected CategoryView(UIFactory uiFactory, Action<CategoryId> callback) : base("GuildSaberCategoryView", [])
    {
        _uiFactory = uiFactory;
        _callback = callback;
        
        OnReady(EventReady);
    }

    public static CategoryView Make(UIFactory uiFactory, Action<CategoryId> callback) =>
        new CategoryView(uiFactory, callback);
    
    private void EventReady(CHOrVLayout x)
    {
        _categoryNameText = _uiFactory.Text(string.Empty);
        _downloadButton = _uiFactory.SecondaryButton("Download");
        _downloadButton
            .OnClick(DownloadClicked)
            .SetWidth(15)
            .SetHeight(4);
        
        _categoryNameText.BuildUI(x.transform);
        _downloadButton.BuildUI(x.transform);
    }

    public void SetData(string categoryName, CategoryId categoryId)
    {
        _categoryId = categoryId;
        
        _categoryNameText.SetText(categoryName);
    }

    private void DownloadClicked()
    {
        _callback(_categoryId);
    }
}