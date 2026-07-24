using System;
using System.Collections.Generic;
using System.Reflection;
using CP_SDK.UI.Components;
using GuildSaber.Mod.Resources;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.Common.UI.Components;

/// <summary>
/// A themed and version-agnostic wrapper around XUIDropdown to allow compatibitily with CP_SDK breaking changes
/// between v6.4.0 and 6.4.4.
/// </summary>
public class GSDropdown : CP_SDK.XUI.XUIDropdown
{
    private readonly TMP_FontAsset _font;

    private readonly MethodInfo _setOptionsVersionAgnostic = typeof(CP_SDK.XUI.XUIDropdown)
        .GetMethod("SetOptions", BindingFlags.Instance | BindingFlags.Public)!;

    public GSDropdown(string name, List<string>? options, TMP_FontAsset font) : base(name, options)
        => (_font, _) = (font, OnReady(ApplyStyle));

    public GSDropdown(List<string>? options, TMP_FontAsset font) : base("GuildSaberDropdown", options)
        => (_font, _) = (font, OnReady(ApplyStyle));

    private static TMP_FontAsset Font => StaticContext.Container
        .ResolveId<TMP_FontAsset>(ResourceMap.TekoMedium);

    private bool? HasTwoParameterSetOptions => field ??= _setOptionsVersionAgnostic.GetParameters().Length == 2;

    public new static XUIDropdown Make(List<string>? options = null) => new(options, Font);
    public new static XUIDropdown Make(string name, List<string>? options = null) => new(options, Font);

    public XUIDropdown SetOptions(List<string> options) => SetOptions(options, true);

    public new XUIDropdown SetOptions(List<string> options, bool notifyOnValueChanged) =>
        (XUIDropdown)_setOptionsVersionAgnostic.Invoke(this, HasTwoParameterSetOptions == true
            ? [options, notifyOnValueChanged]
            : [options]);

    private void ApplyStyle(CDropdown element)
    {
        element.transform.Find("BG")
            .GetComponent<ImageView>()
            .color = new Color(0.0f, 0.0f, 0.0f, 0.9f);

        foreach (var text in element.GetComponentsInChildren<TextMeshProUGUI>()) text.font = _font;
    }

    public new XUIDropdown OnValueChanged(Action<int, string> functor, bool add = true)
        => (XUIDropdown)OnReady(x => x.OnValueChanged(functor, add));

    public new XUIDropdown SetActive(bool active) => (XUIDropdown)OnReady(x => x.gameObject.SetActive(active));

    public new XUIDropdown SetInteractable(bool interactable)
        => (XUIDropdown)OnReady(x => x.SetInteractable(interactable));

    public new XUIDropdown SetValue(string value, bool notify = true)
        => (XUIDropdown)OnReady(x => x.SetValue(value, notify));

    public XUIDropdown Bind(ref XUIDropdown target) => target = this;
}