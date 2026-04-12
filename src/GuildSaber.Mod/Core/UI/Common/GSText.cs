using System.Linq;
using CP_SDK.UI.Components;
using CP_SDK.XUI;
using TMPro;
using Zenject;

namespace GuildSaber.Mod.Core.UI.Common;

public class GSText : XUIText
{
    [Inject] protected GsUiResources Resources = null!;
    
    protected GSText(string name, string text) : base(name, text)
    {
        OnReady(PatchText);
    }

    public new static GSText Make(string text)
        => new("GuildSaberText", text);

    public GSText Bind(ref GSText value)
    {
        value = this;
        return this;
    }

    public void PatchText(CText text)
    {
        PatchText(text.GetComponentInChildren<TextMeshProUGUI>());
    }

    public void PatchText(TextMeshProUGUI text)
    {
        text.font = Resources.TekoFont;
    }

////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////

    public new GSText SetMargins(float left, float top, float right, float bottom)
    {
        base.SetMargins(left, top, right, bottom);
        return this;
    }

    public GSText SetUseGradient(bool value)
    {
        OnReady(x =>
        {
            x.GetComponentInChildren<TextMeshProUGUI>().enableVertexGradient = value;
        });
        return this;
    }

    public GSText SetGradient(VertexGradient gradient)
    {
        SetUseGradient(true);
        OnReady(x =>
        {
            x.GetComponentInChildren<TextMeshProUGUI>().colorGradient = gradient;
        });
        return this;
    }
}