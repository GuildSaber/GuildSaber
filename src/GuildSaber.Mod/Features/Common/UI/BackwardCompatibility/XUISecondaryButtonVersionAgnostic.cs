using System;
using UnityEngine;

namespace GuildSaber.Mod.Features.Common.UI.BackwardCompatibility;

/// <summary>
/// A version-agnostic wrapper around XUIDropdown to allow compatibitily with CP_SDK breaking changes between v6.4.0 and
/// 6.4.4.
/// </summary>
public class XUISecondaryButtonVersionAgnostic : CP_SDK.XUI.XUISecondaryButton
{
    protected XUISecondaryButtonVersionAgnostic(string name, string label, Action? onClick = null)
        : base(name, label, onClick) { }

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