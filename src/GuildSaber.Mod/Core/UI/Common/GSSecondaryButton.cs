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

    private        int        _height;
    private        int        _width;

    [Inject] protected GsUiResources GsResources = null!;
    
    protected GSSecondaryButton(string label, Action? onClick = null) : base("GuildSaberSecondaryButton", label, onClick)
    {
        OnReady(_SetupStyle);
    }


    protected GSSecondaryButton(string name, string label, int width, int height, Action? onClick = null) : base(name, label, onClick)
    {
        OnReady(_SetupStyle);
        _width  = width;
        _height = height;
    }

    public new static GSSecondaryButton Make(string label, Action? onClick = null)
        => new(label, onClick);

    public static GSSecondaryButton Make(string label, int width, int height, string name = "GuildSaberSecondaryButton", Action? onClick = null)
        => new(name, label, width, height, onClick);
#nullable disable
    public virtual Color GetColor() => Color.black.ColorWithAlpha(0.7f);

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    private void _SetupStyle(CSecondaryButton button)
    {
        SetupStyle(button, _width, _height, GetColor());
    }

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
        var l_Tex = new Texture2D(width * 7, height * 7);

        for (var l_X = 0; l_X < l_Tex.width; l_X++)
        {
            for (var l_Y = 0; l_Y < l_Tex.height; l_Y++)
            {
                l_Tex.SetPixel(l_X, l_Y, Color.white);
            }
        }

        var l_NewTex = await TextureUtils.CreateRoundedTextureAsync( /*await Utils.TextureUtils.Gradient(l_Tex, new Color(1, 1, 1, 0.7f), new Color(1f, 1f, 1f, 1), p_UseAlpha: true)*/l_Tex, 10);
        var l_Sprite = Sprite.Create(l_NewTex, new Rect(0, 0, l_Tex.width, l_Tex.height), new Vector2(0, 0), 1000, 0, SpriteMeshType.FullRect);
        return l_Sprite;
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public GSSecondaryButton SetWidth(int width, bool refreshVisuals = false)
    {
        _width = width;
        base.SetWidth(width);

        if (refreshVisuals)
        {
            OnReady(_SetupStyle);
        }

        return this;
    }

    public GSSecondaryButton SetHeight(int height, bool refreshVisuals = false)
    {
        _height = height;
        base.SetHeight(height);

        if (refreshVisuals)
        {
            OnReady(_SetupStyle);
        }

        return this;
    }

    public GSSecondaryButton Bind(ref GSSecondaryButton x)
    {
        x = this;
        return this;
    }
}