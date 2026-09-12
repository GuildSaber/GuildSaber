using System;
using System.Collections.Generic;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using HMUI;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

public sealed class PlayerCardSettings : SimpleFlowCoordinator
{
    [Inject] private readonly PlayerCardSettingsView _view = null!;

    protected override string Title => "Player card settings";
    protected override ViewController GetMainViewController() => _view;

    public event Action<PlayerCardSettingsMessage>? SettingsChanged
    {
        add => _view.MessageSent += value;
        remove => _view.MessageSent -= value;
    }

    public void Render(PlayerCardSettingsState state) => _view.Render(state);
}

internal sealed class PlayerCardSettingsView : ViewController<PlayerCardSettingsView>
{
    private readonly List<string> _colorModes = ["Automatic", "Solid", "Gradient"];
    private XUIDropdown _colorMode = null!;
    private XUIHLayout _colorModeLayout = null!;
    private XUIToggle _enabled = null!;
    private XUIHLayout _gradientColorsLayout = null!;
    private XUIColorInput _gradientEnd = null!;
    private XUIColorInput _gradientStart = null!;
    private bool _isReady;
    private XUIColorInput _mainColor = null!;
    private XUIHLayout _mainColorLayout = null!;
    private PlayerCardSettingsState? _pendingState;
    private XUIToggle _showHandle = null!;
    private XUIToggle _showOrderedAchievements = null!;

    public event Action<PlayerCardSettingsMessage>? MessageSent;

    public void Render(PlayerCardSettingsState state)
    {
        if (!_isReady)
        {
            _pendingState = state;
            return;
        }

        _enabled.SetValue(state.Enabled, false);
        _showOrderedAchievements.SetValue(state.ShowOrderedAchievements, false);
        _showHandle.SetValue(state.ShowHandle, false);
        _colorMode.SetValue(_colorModes[(int)state.ColorMode], false);
        _mainColor.SetValue(state.MainColor, false);
        _gradientStart.SetValue(state.GradientStart, false);
        _gradientEnd.SetValue(state.GradientEnd, false);
        _colorModeLayout.SetActive(state.CanCustomize);
        _mainColorLayout.SetActive(state is { CanCustomize: true, ColorMode: not PlayerCardColorMode.Automatic });
        _gradientColorsLayout.SetActive(state is { CanCustomize: true, ColorMode: PlayerCardColorMode.Gradient });
    }

    protected override void OnViewCreation()
    {
        Templates.FullRectLayoutMainView(
                XUIHLayout.Make(
                    Toggle("Enable player card: ", ref _enabled,
                        value => new PlayerCardSettingsMessage.SetEnabled(value)),
                    Toggle(
                        "Show category levels: ",
                        ref _showOrderedAchievements,
                        value => new PlayerCardSettingsMessage.SetShowOrderedAchievements(value))),
                Toggle("Show handle: ", ref _showHandle,
                    value => new PlayerCardSettingsMessage.SetShowHandle(value)),
                XUIHLayout.Make(
                        XUIText.Make("Color mode: "),
                        XUIDropdown.Make()
                            .Bind(ref _colorMode)
                            .SetOptions(_colorModes)
                            .OnValueChanged((index, _) => Send(
                                new PlayerCardSettingsMessage.SetColorMode((PlayerCardColorMode)index))))
                    .Bind(ref _colorModeLayout),
                XUIHLayout.Make(
                        XUIText.Make("Accent color: "),
                        XUIColorInput.Make()
                            .Bind(ref _mainColor)
                            .OnValueChanged(value => Send(new PlayerCardSettingsMessage.SetMainColor(value))))
                    .Bind(ref _mainColorLayout),
                XUIHLayout.Make(
                        XUIText.Make("Gradient start: "),
                        XUIColorInput.Make()
                            .Bind(ref _gradientStart)
                            .OnValueChanged(value => Send(new PlayerCardSettingsMessage.SetGradientStart(value))),
                        XUIText.Make("Gradient end: "),
                        XUIColorInput.Make()
                            .Bind(ref _gradientEnd)
                            .OnValueChanged(value => Send(new PlayerCardSettingsMessage.SetGradientEnd(value))))
                    .Bind(ref _gradientColorsLayout),
                XUISecondaryButton.Make("Reset timer")
                    .SetWidth(40).SetHeight(5)
                    .OnClick(() => Send(new PlayerCardSettingsMessage.ResetTimer())),
                XUIHLayout.Make(
                    XUISecondaryButton.Make("Reset menu position")
                        .SetWidth(40).SetHeight(5)
                        .OnClick(() => Send(new PlayerCardSettingsMessage.ResetMenuPosition())),
                    XUISecondaryButton.Make("Reset in-song position")
                        .SetWidth(40).SetHeight(5)
                        .OnClick(() => Send(new PlayerCardSettingsMessage.ResetGameplayPosition()))))
            .BuildUI(transform);

        _isReady = true;
        if (_pendingState is { } state) Render(state);
    }

    private XUIHLayout Toggle(string label, ref XUIToggle toggle, Func<bool, PlayerCardSettingsMessage> message)
        => XUIHLayout.Make(
            XUIText.Make(label),
            XUIToggle.Make()
                .Bind(ref toggle)
                .OnValueChanged(value => Send(message(value)))
        );

    private void Send(PlayerCardSettingsMessage message) => MessageSent?.Invoke(message);
}

public abstract record PlayerCardSettingsMessage
{
    public sealed record SetEnabled(bool Value) : PlayerCardSettingsMessage;
    public sealed record SetShowOrderedAchievements(bool Value) : PlayerCardSettingsMessage;
    public sealed record SetShowHandle(bool Value) : PlayerCardSettingsMessage;
    public sealed record SetColorMode(PlayerCardColorMode Value) : PlayerCardSettingsMessage;
    public sealed record SetMainColor(Color Value) : PlayerCardSettingsMessage;
    public sealed record SetGradientStart(Color Value) : PlayerCardSettingsMessage;
    public sealed record SetGradientEnd(Color Value) : PlayerCardSettingsMessage;
    public sealed record ResetTimer : PlayerCardSettingsMessage;
    public sealed record ResetMenuPosition : PlayerCardSettingsMessage;
    public sealed record ResetGameplayPosition : PlayerCardSettingsMessage;
}
