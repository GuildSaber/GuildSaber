using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.UI;
using UnityEngine;
using Zenject;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Settings;

public class PlayerCardSettingsMainView : ViewController<PlayerCardSettingsMainView>
{
    [Inject(Id = Constants.CardFloatingPanelId)]
    private readonly FloatingScreen _cardScreen = null!;

    [Inject] private readonly PlayerCardView _cardView = null!;
    [Inject] private readonly PluginConfig _config = null!;

    [Inject] private readonly GuildSaberCache _guildSaberCache = null!;
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
                    _uiFactory.Text("Main color: "),
                    XUIColorInput.Make()
                        .OnValueChanged(OnColorChanged)
                        .Bind(ref _mainColorInput)
                ),
                XUIHLayout.Make(
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
            XUIHLayout.Make(
                _uiFactory.SecondaryButton("Reset menu position", 40, 5)
                    .OnClick(ResetMenuPosition),
                _uiFactory.SecondaryButton("Reset in song position", 40, 5)
                    .OnClick(ResetInSongPosition)
            )
        ).BuildUI(transform);

        LoadConfig();
    }

    private void ResetMenuPosition()
    {
        _config.PlayerCard.Transforms.Menu = new CardConfig().Transforms.Menu;
        _cardView.SetCardToMenuTransform();
    }

    private void ResetInSongPosition() => _config.PlayerCard.Transforms.InSong = new CardConfig().Transforms.InSong;

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
        if (_guildSaberCache.PlayerExtended is not null
            && _guildSaberCache.MemberLevelStats.TryGetValue(_config.PlayerCard.ContextId, out var memberLevelStat))
        {
            var canPlayerUseColors = PlayerCardLibrary.CanPlayerUseCustomColors(
                memberLevelStat,
                _guildSaberCache.PlayerExtended.Player);

            if (canPlayerUseColors)
            {
                _useCustomColorsLayout.SetActive(true);
                _useGradientLayout.SetActive(_config.PlayerCard.ColorSettings.UseCustomColors);
                _customColorsLayout.SetActive(_config.PlayerCard.ColorSettings.UseCustomColors);

                return;
            }
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