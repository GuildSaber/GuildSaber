using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.Timer;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Settings;

public class PlayerCardSettingsMainView : ViewController<PlayerCardSettingsMainView>
{
    [Inject(Id = Constants.CardFloatingPanelId)]
    private readonly FloatingScreen _cardScreen = null!;

    [Inject] private readonly PlayerCardView _cardView = null!;
    [Inject] private readonly GuildSaberConfig _config = null!;
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;

    [Inject] private readonly GuildSaberSession _session = null!;
    [Inject] private readonly Timer _timer = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;
    private XUIColorInput _color0Input = null!;
    private XUIColorInput _color1Input = null!;
    private XUIVLayout _customColorsLayout = null!;

    private XUIToggle _displayLevelDetailsToggle = null!;

    private XUIColorInput _mainColorInput = null!;
    private XUIToggle _showHandleToggle = null!;

    private XUIHLayout _useCustomColorsLayout = null!;
    private XUIToggle _useCustomColorsToggle = null!;
    private XUIHLayout _useGradientLayout = null!;
    private XUIToggle _useGradientToggle = null!;

    public event Action OnResetMenuPosition = () => { };
    public event Action OnResetInSongPosition = () => { };

    protected override void OnViewCreation()
    {
        Templates.FullRectLayoutMainView(
            XUIHLayout.Make(
                _uiFactory.Text("Display level details: "),
                XUIToggle.Make()
                    .OnValueChanged(OnToggleChanged)
                    .Bind(ref _displayLevelDetailsToggle)
            ),
            XUIHLayout.Make(
                _uiFactory.Text("Show handle: "),
                XUIToggle.Make()
                    .OnValueChanged(OnToggleChanged)
                    .Bind(ref _showHandleToggle)
            ),
            XUIHLayout.Make(
                _uiFactory.Text("Use custom colors:"),
                XUIToggle.Make()
                    .OnValueChanged(OnToggleChanged)
                    .Bind(ref _useCustomColorsToggle)
            ).Bind(ref _useCustomColorsLayout),
            XUIHLayout.Make(
                _uiFactory.Text("Use gradient: "),
                XUIToggle.Make()
                    .OnValueChanged(OnToggleChanged)
                    .Bind(ref _useGradientToggle)
            ).Bind(ref _useGradientLayout),
            XUIVLayout.Make(
                XUIHLayout.Make(
                    XUIHLayout.Make(
                        _uiFactory.Text("Main color: "),
                        XUIColorInput.Make()
                            .OnValueChanged(OnColorChanged)
                            .Bind(ref _mainColorInput)
                    ),
                    XUIHLayout.Make(
                        _uiFactory.Text("Color 0: "),
                        XUIColorInput.Make()
                            .OnValueChanged(OnColorChanged)
                            .Bind(ref _color0Input)
                    ),
                    XUIHLayout.Make(
                        _uiFactory.Text("Color 1: "),
                        XUIColorInput.Make()
                            .OnValueChanged(OnColorChanged)
                            .Bind(ref _color1Input)
                    )
                )
            ).Bind(ref _customColorsLayout),
            _uiFactory.SecondaryButton("Reset timer")
                .SetWidth(40)
                .SetHeight(5)
                .OnClick(ResetTimer),
            XUIHLayout.Make(
                _uiFactory.SecondaryButton("Reset menu position")
                    .SetWidth(40)
                    .SetHeight(5)
                    .OnClick(OnResetMenuPosition),
                _uiFactory.SecondaryButton("Reset in-song position")
                    .SetWidth(40)
                    .SetHeight(5)
                    .OnClick(OnResetInSongPosition)
            )
        ).BuildUI(transform);

        LoadConfig();
    }

    private void ResetTimer()
    {
        _config.PlayerCard.TimerConfig.PlayDurationSec = 0;
        _timer.Reset();
    }

    private void OnToggleChanged(bool value)
    {
        _config.PlayerCard.CategoryLevelViewEnabled = _displayLevelDetailsToggle.Element.GetValue();
        _config.PlayerCard.ColorSettings.UseCustomColors = _useCustomColorsToggle.Element.GetValue();
        _config.PlayerCard.ColorSettings.UseGradient = _useGradientToggle.Element.GetValue();

        UpdateUI();
        UpdateCard();
    }

    private void OnColorChanged(Color value)
    {
        _config.PlayerCard.ColorSettings.MainCardColor = _mainColorInput.Element.GetValue();
        _config.PlayerCard.ColorSettings.GradientColor0 = _color0Input.Element.GetValue();
        _config.PlayerCard.ColorSettings.GradientColor1 = _color1Input.Element.GetValue();

        UpdateUI();
        UpdateCard();
    }

    private void LoadConfig()
    {
        _useCustomColorsToggle.SetValue(_config.PlayerCard.ColorSettings.UseCustomColors, false);
        _useGradientToggle.SetValue(_config.PlayerCard.ColorSettings.UseGradient, false);
        _mainColorInput.SetValue(_config.PlayerCard.ColorSettings.MainCardColor, false);
        _color0Input.SetValue(_config.PlayerCard.ColorSettings.GradientColor0, false);
        _color1Input.SetValue(_config.PlayerCard.ColorSettings.GradientColor1, false);
        _displayLevelDetailsToggle.SetValue(_config.PlayerCard.CategoryLevelViewEnabled, false);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_guildSaberManager.Initialized
            && _session.TryGetCurrentMemberLevelStats(out var memberLevelStat)
            && PlayerCardLibrary.CanPlayerUseCustomColors(memberLevelStat, _session.PlayerExtended.Player))
        {
            _useCustomColorsLayout.SetActive(true);
            _useGradientLayout.SetActive(_config.PlayerCard.ColorSettings.UseCustomColors);
            _customColorsLayout.SetActive(_config.PlayerCard.ColorSettings.UseCustomColors);

            return;
        }

        _useCustomColorsLayout.SetActive(false);
        _customColorsLayout.SetActive(false);
        _useGradientLayout.SetActive(false);
    }

    private void UpdateCard()
    {
        _cardScreen.ShowHandle = _showHandleToggle.Element.GetValue();
        _cardView.LoadConfig();
    }
}