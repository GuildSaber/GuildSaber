using CP_SDK.XUI;
using TMPro;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSText : XUIText
{
    public GSText(string text, TMP_FontAsset font) : base("GuildSaberText", text)
        => OnReady(element => element.TMProUGUI.font = font);

    public GSText Bind(ref GSText value) => value = this;

    public GSText SetGradiantEnabled(bool value)
    {
        OnReady(x => x.TMProUGUI.enableVertexGradient = value);
        return this;
    }

    public GSText SetGradient(VertexGradient gradient)
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