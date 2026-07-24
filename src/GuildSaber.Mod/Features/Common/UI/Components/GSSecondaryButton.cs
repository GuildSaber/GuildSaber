using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CP_SDK.UI.Components;
using GuildSaber.Mod.Helpers;
using GuildSaber.Mod.Resources;
using TMPro;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI.Components;

[SuppressMessage("ReSharper", "AsyncVoidMethod")]
public class GSSecondaryButton : CP_SDK.XUI.XUISecondaryButton
{
    private readonly TMP_FontAsset _font;

    public GSSecondaryButton(string name, string label, TMP_FontAsset font, Action? onClick = null)
        : base(name, label, onClick) => _font = font;

    public GSSecondaryButton(string label, TMP_FontAsset font, Action? onClick = null)
        : base("GuildSaberSecondaryButton", label, onClick) => _font = font;

    private static TMP_FontAsset Font => StaticContext.Container
        .ResolveId<TMP_FontAsset>(ResourceMap.TekoMedium);

    public float Width => Element.LElement.preferredWidth;
    public float Height => Element.LElement.preferredHeight;

    public new static XUISecondaryButton Make(string label, Action? onClick = null)
        => new(label, Font, onClick);

    public new static XUISecondaryButton Make(string name, string label, Action? onClick = null)
        => new(name, label, Font, onClick);

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
        button.gameObject.GetComponentInChildren<TextMeshProUGUI>().font = _font;
    }

    public virtual Color GetColor() => Color.black.WithAlpha(0.7f);

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

    /// <remarks>This function didn't exist on 6.4.0</remarks>
    public new XUISecondaryButton SetColor(Color color)
        => (XUISecondaryButton)OnReady(x => x.TextC.SetColor(color));

    public new XUISecondaryButton SetWidth(float width) => (XUISecondaryButton)OnReady(x => x.SetWidth(width));
    public new XUISecondaryButton SetHeight(float height) => (XUISecondaryButton)OnReady(x => x.SetHeight(height));
    public new XUISecondaryButton SetText(string text) => (XUISecondaryButton)OnReady(x => x.SetText(text));

    public new XUISecondaryButton SetFontSize(float fontSize)
        => (XUISecondaryButton)OnReady(x => x.SetFontSize(fontSize));

    public new XUISecondaryButton OnClick(Action functor, bool add = true)
        => (XUISecondaryButton)OnReady(x => x.OnClick(functor, add));

    public new XUISecondaryButton SetInteractable(bool interactable)
        => (XUISecondaryButton)OnReady(x => x.SetInteractable(interactable));

    public new XUISecondaryButton SetActive(bool active)
        => (XUISecondaryButton)OnReady(x => x.gameObject.SetActive(active));

    public new XUISecondaryButton SetBackgroundColor(Color color)
        => (XUISecondaryButton)OnReady(x => x.SetBackgroundColor(color));

    public XUISecondaryButton Bind(ref XUISecondaryButton target) => target = this;
}