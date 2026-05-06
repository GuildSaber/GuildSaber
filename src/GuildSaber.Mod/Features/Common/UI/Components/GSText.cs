using CP_SDK.UI.Components;
using CP_SDK.XUI;
using TMPro;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSText : XUIText
{
    protected readonly TMP_FontAsset Font;

    public GSText(string text, TMP_FontAsset font) : base("GuildSaberText", text)
    {
        Font = font;
        OnReady(PatchText);
    }

    public GSText Bind(ref GSText value)
    {
        value = this;
        return this;
    }

    public void PatchText(CText text) => PatchText(text.GetComponentInChildren<TextMeshProUGUI>());
    public void PatchText(TextMeshProUGUI text) => text.font = Font;

    ////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////

    public new GSText SetMargins(float left, float top, float right, float bottom)
    {
        base.SetMargins(left, top, right, bottom);
        return this;
    }

    public GSText SetUseGradient(bool value)
    {
        OnReady(x => { x.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = value; });
        return this;
    }

    public GSText SetGradient(VertexGradient gradient)
    {
        SetUseGradient(true);
        OnReady(x => { x.GetComponentInChildren<TextMeshProUGUI>().colorGradient = gradient; });
        return this;
    }
}