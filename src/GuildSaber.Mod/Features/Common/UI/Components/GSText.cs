using CP_SDK.UI.Components;
using CP_SDK.XUI;
using TMPro;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSText : XUIText
{
    protected readonly TMP_FontAsset Font;

    public GSText(string text, TMP_FontAsset font) : base("GuildSaberText", text)
        => (Font, _) = (font, OnReady(PatchText));

    public GSText Bind(ref GSText value) => value = this;

    private void PatchText(CText text) => PatchText(text.GetComponentInChildren<TextMeshProUGUI>());
    private void PatchText(TextMeshProUGUI text) => text.font = Font;

    public GSText SetGradiantEnabled(bool value)
    {
        OnReady(x => x.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = value);
        return this;
    }

    public GSText SetGradient(VertexGradient gradient)
    {
        OnReady(x =>
        {
            var textComponent = x.GetComponentInChildren<TextMeshProUGUI>();

            textComponent.enableVertexGradient = true;
            textComponent.colorGradient = gradient;
        });
        return this;
    }
}