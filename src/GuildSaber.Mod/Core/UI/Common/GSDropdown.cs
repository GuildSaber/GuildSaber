using CP_SDK.XUI;
using HMUI;
using TMPro;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Common;

public class GSDropdown : XUIDropdown
{
    public GSDropdown(TMP_Asset font) : base("GuildSaberDropdown", []) => OnReady(x =>
    {
        var imageView = x.transform.Find("BG").GetComponent<ImageView>();

        imageView.color = new Color(0.0f, 0.0f, 0.0f, 0.9f);
    });

    public GSDropdown Bind(ref GSDropdown target)
    {
        target = this;
        return this;
    }
}