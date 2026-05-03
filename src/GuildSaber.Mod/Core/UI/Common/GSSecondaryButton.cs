using System;
using System.Threading.Tasks;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.Core.UI.Utils;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Common;

public class GSSecondaryButton : XUISecondaryButton
{
    protected readonly TMP_FontAsset Font;
    private int _height;
    private int _width;

    public GSSecondaryButton(string label, TMP_FontAsset font, Action? onClick = null)
        : base("GuildSaberSecondaryButton", label, onClick)
    {
        Font = font;
        OnReady(_SetupStyle);
    }

    public GSSecondaryButton(string label, int width, int height, TMP_FontAsset font, Action? onClick = null)
        : base("GuildSaberSecondaryButton", label, onClick)
    {
        Font = font;
        _width = width;
        _height = height;

        OnReady(_SetupStyle);
    }


    public virtual Color GetColor() => Color.black.ColorWithAlpha(0.7f);

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private void _SetupStyle(CSecondaryButton button) => SetupStyle(button, _width, _height, GetColor());

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public async void SetupStyle(CSecondaryButton button, int width, int height, Color color)
    {
        button.SetWidth(width);
        button.SetHeight(height);

        var sprite = await GetBackground(width, height);
        button.SetBackgroundColor(color);
        button.SetBackgroundSprite(sprite);
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().font = Font;
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public static async Task<Sprite> GetBackground(int width, int height)
    {
        var texture = new Texture2D(width * 7, height * 7);

        for (var x = 0; x < texture.width; x++)
        for (var y = 0; y < texture.height; y++)
            texture.SetPixel(x, y, Color.white);

        var roundedTexture = await TextureUtils.CreateRoundedTextureAsync(texture, 10);

        return Sprite.Create(
            roundedTexture,
            new Rect(0, 0, texture.width, texture.height),
            pivot: new Vector2(0, 0),
            pixelsPerUnit: 1000,
            extrude: 0,
            meshType: SpriteMeshType.FullRect
        );
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public GSSecondaryButton SetWidth(int width, bool refreshVisuals = false)
    {
        _width = width;
        base.SetWidth(width);

        if (refreshVisuals) OnReady(_SetupStyle);

        return this;
    }

    public GSSecondaryButton SetHeight(int height, bool refreshVisuals = false)
    {
        _height = height;
        base.SetHeight(height);

        if (refreshVisuals) OnReady(_SetupStyle);

        return this;
    }

    public GSSecondaryButton Bind(ref GSSecondaryButton x)
    {
        x = this;
        return this;
    }
}