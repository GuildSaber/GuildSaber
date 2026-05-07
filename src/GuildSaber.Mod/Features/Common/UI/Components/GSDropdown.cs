using CP_SDK.XUI;
using HMUI;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI.Components;

public class GSDropdown : XUIDropdown
{
    private readonly TMP_FontAsset _font;

    public GSDropdown(TMP_FontAsset font) : base("GuildSaberDropdown", []) => (_font, _) = (font, OnReady(element =>
    {
        element.transform.Find("BG").GetComponent<ImageView>()
            .color = new Color(0.0f, 0.0f, 0.0f, 0.9f);

        foreach (var text in element.GetComponentsInChildren<TextMeshProUGUI>())
            text.font = _font;
    }));

    public GSDropdown Bind(ref GSDropdown target) => target = this;
}