using System;
using System.Threading.Tasks;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.Core.UI.Utils;
using TMPro;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Common;

public class GSSecondaryButton : XUISecondaryButton
{
    private int _height;
    private int _width;

    [Inject] protected GsUiResources GsResources = null!;

    protected GSSecondaryButton(string label, Action? onClick = null) : base("GuildSaberSecondaryButton", label,
        onClick)
        => OnReady(_SetupStyle);

    protected GSSecondaryButton(string name, string label, int width, int height, Action? onClick = null)
        : base(name, label, onClick)
    {
        OnReady(_SetupStyle);
        _width = width;
        _height = height;
    }

    public new static GSSecondaryButton Make(string label, Action? onClick = null)
        => new(label, onClick);

    public static GSSecondaryButton Make(string label, int width, int height, string name = "GuildSaberSecondaryButton",
                                         Action? onClick = null)
        => new(name, label, width, height, onClick);

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

        var l_Sprite = await GetBackground(width, height);
        button.SetBackgroundColor(color);
        button.SetBackgroundSprite(l_Sprite);
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().font = GsResources.TekoFont;
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public static async Task<Sprite> GetBackground(int width, int height)
    {
        var texture = new Texture2D(width * 7, height * 7);

        for (var x = 0; x < texture.width; x++)
        for (var y = 0; y < texture.height; y++)
            texture.SetPixel(x, y, Color.white);

        var newTexture = await TextureUtils.CreateRoundedTextureAsync(texture, 10);
        //await Utils.TextureUtils.Gradient(l_Tex, new Color(1, 1, 1, 0.7f), new Color(1f, 1f, 1f, 1), p_UseAlpha: true)

        return Sprite.Create(
            newTexture,
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