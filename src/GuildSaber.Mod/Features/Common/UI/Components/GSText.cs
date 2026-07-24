using GuildSaber.Mod.Resources;
using TMPro;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSText : CP_SDK.XUI.XUIText
{
    public GSText(string name, string text, TMP_FontAsset font) : base(name, text)
        => OnReady(element => element.TMProUGUI.font = font);

    public GSText(string text, TMP_FontAsset font) : base("GuildSaberText", text)
        => OnReady(element => element.TMProUGUI.font = font);

    private static TMP_FontAsset Font => StaticContext.Container
        .ResolveId<TMP_FontAsset>(ResourceMap.TekoMedium);

    public new static XUIText Make(string text) => new(text, Font);
    public new static XUIText Make(string name, string text) => new(name, text, Font);

    public XUIText Bind(ref GSText value) => value = this;

    public XUIText SetGradiantEnabled(bool value)
    {
        OnReady(x => x.TMProUGUI.enableVertexGradient = value);
        return this;
    }

    public XUIText SetGradient(VertexGradient gradient)
    {
        OnReady(element =>
        {
            var textComponent = element.TMProUGUI;

            textComponent.enableVertexGradient = true;
            textComponent.colorGradient = gradient;
        });

        return this;
    }
}