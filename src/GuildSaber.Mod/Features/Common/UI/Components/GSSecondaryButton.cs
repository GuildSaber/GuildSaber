using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using GuildSaber.Mod.Helpers;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI.Components;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GSSecondaryButton(string label, TMP_FontAsset font, Action? onClick = null)
    : XUISecondaryButton("GuildSaberSecondaryButton", label, onClick)
{
    public float Width => Element.LElement.preferredWidth;
    public float Height => Element.LElement.preferredHeight;

    public override void BuildUI(Transform parent)
    {
        OnReady(SetupStyle);
        base.BuildUI(parent);
    }

    private void SetupStyle(CSecondaryButton button) => _SetupStyle(button, GetColor());

    private async void _SetupStyle(CSecondaryButton button, Color color)
    {
        var width = (int)Width;
        var height = (int)Height;

        if (width <= 0 || height <= 0)
            return;

        var sprite = await GetBackground((int)Width, (int)Height);
        button.SetBackgroundColor(color);
        button.SetBackgroundSprite(sprite);
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().font = font;
    }

    public virtual Color GetColor() => Color.black.ColorWithAlpha(0.7f);


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


    public GSSecondaryButton Bind(ref GSSecondaryButton x) => x = this;
}