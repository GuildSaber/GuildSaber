using System.Collections.Generic;
using System.Reflection;

namespace GuildSaber.Mod.Features.Common.UI.BackwardCompatibility;

/// <summary>
/// A version-agnostic wrapper around XUIDropdown to allow compatibitily with CP_SDK breaking changes between v6.4.0 and
/// 6.4.4.
/// </summary>
public class XUIDropdownVersionAgnostic : CP_SDK.XUI.XUIDropdown
{
    protected XUIDropdownVersionAgnostic(string name, List<string> options) : base(name, options) { }

    private bool? HasTwoParameterSetOptions => field ??= _setOptionsVersionAgnostic.GetParameters().Length == 2;

    private readonly MethodInfo _setOptionsVersionAgnostic = typeof(CP_SDK.XUI.XUIDropdown)
        .GetMethod("SetOptions", BindingFlags.Instance | BindingFlags.Public)!;

    public XUIDropdown SetOptions(List<string> options) => SetOptions(options, true);

    public new XUIDropdown SetOptions(List<string> options, bool notifyOnValueChanged) =>
        (XUIDropdown)_setOptionsVersionAgnostic.Invoke(this, HasTwoParameterSetOptions == true
            ? [options, notifyOnValueChanged]
            : [options]);

    public XUIDropdown Bind(ref XUIDropdown target) => target = this;
}