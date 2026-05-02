using System;
using System.Linq;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.Game;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.PlayerCard.UI.Components;
using GuildSaber.Mod.Core.PlayerCard.UI.Settings;
using GuildSaber.Mod.Core.Time;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.Core.UI.Guild;
using GuildSaber.Mod.Core.UI.Utils;
using GuildSaber.Mod.Extensions;
using GuildSaber.Mod.Installers;
using GuildSaber.Mod.Resources;
using HMUI;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Color = UnityEngine.Color;
using Logger = IPA.Logging.Logger;

namespace GuildSaber.Mod.Core.PlayerCard.UI;

internal class PlayerCardView : ViewController<PlayerCardView>
{
    public enum EDisplayMode
    {
        Normal,
        Settings,
        Loading,
        Error
    }

    [Inject(Id = Constants.CardFloatingPanelId)]
    private readonly FloatingScreen _cardFloatingScreen = null!;

    [Inject] private readonly PlayerCardSettingsCoordinator _cardSettingsCoordinator = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    [Inject] private readonly SiraLog _logger = null!;
    [Inject] private readonly ModData _modData = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;
    [Inject] private readonly TimeController _timeControl = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;
    [Inject] private readonly GuildSelectionFlowCoordinator _guildSelectionFlowCoordinator = null!;

    [Inject(Id = nameof(ResourceMap.DownArrow))]
    private readonly Texture2D _downArrowTexture = null!;

    [Inject(Id = nameof(ResourceMap.GsWhiteLogo))]
    private readonly Texture2D _whiteLogoTexture = null!;

    private ImageView _borderImage = null!;

    protected GSText GuildWarningMessageText = null!;
    protected XUIVLayout InvalidConfigLayout = null!;
    protected XUIVLayout LoadingLayout = null!;
    protected XUIVLayout ServerUnreachableLayout = null!;

    protected XUIHLayout MainLayout = null!;

    protected PagedLevelList MainPlayerLevelsContainer = null!;
    protected XUIVLayout PlayerDataContainer = null!;

    protected XUIIconButton PlayerImage = null!;

    protected XUIVLayout PlayerImageContainer = null!;
    protected GSText PlayerLevelText = null!;

    protected GSText PlayerNameText = null!;
    protected GSText PlayerPassesText = null!;

    protected PointList PointsContainer = null!;
    protected GSSecondaryButton ShowSettingsButton = null!;
    protected GSText TimeText = null!;

    protected GuildSelector GuildSelector = null!;

    protected override void OnViewCreation()
    {
        XUIVLayout.Make(
                _uiFactory.Text("Please select a guild to use the Player Card")
                    .Bind(ref GuildWarningMessageText)
                    .SetColor(Color.yellow),
                GuildSelector.Make(new GuildSelector.GuildSelectorParams(_guildSelectionFlowCoordinator,
                        DownArrowTexture: _downArrowTexture,
                        WhiteArrowTexture: _whiteLogoTexture, _modData, _guildSaberManager))
                    .Bind(ref GuildSelector)
                    .SetOnGuildSelected(EventGuildSelected),
                _uiFactory.SecondaryButton("Show settings")
                    .Bind(ref ShowSettingsButton)
                    .SetWidth(20)
                    .SetHeight(5)
                    .OnClick(DisplaySettings),
                _uiFactory.SecondaryButton("Reset timer")
                    .SetWidth(20)
                    .SetHeight(5)
                    .OnClick(ResetTimer)
            )
            .Bind(ref InvalidConfigLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
                _uiFactory.Text("Server unreachable.\nOr you're not registered on the website")
                    .SetColor(new Color(1, 0.5f, 0)),
                _uiFactory.SecondaryButton("Open in browser", 40, 4)
                    .OnClick(() =>
                        System.Diagnostics.Process.Start(_config.ApiEnv.ToWebsiteUri.ToString())
                    )
            )
            .Bind(ref ServerUnreachableLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
                _uiFactory.Text("Loading... TODO: Replace this text by the loading indicator of the base game")
            ).Bind(ref LoadingLayout)
            .BuildUI(transform);

        XUIHLayout.Make(
                XUIVLayout.Make(
                        _uiFactory.Text(string.Empty)
                            .Bind(ref PlayerNameText)
                            .SetStyle(FontStyles.Underline | FontStyles.Bold)
                            .SetFontSize(5),
                        _uiFactory.Text(string.Empty)
                            .Bind(ref PlayerPassesText)
                            .SetFontSize(3.7f),
                        _uiFactory.Text(string.Empty)
                            .Bind(ref PlayerLevelText)
                            .SetFontSize(4.5f),
                        PointList.Make(_modData, _uiFactory)
                            .Bind(ref PointsContainer)
                            .SetSpacing(0),
                        _uiFactory.Text("______")
                            .SetFontSize(3),
                        _uiFactory.Text("00:00:00")
                            .Bind(ref TimeText)
                    )
                    .SetPadding(2, 2, 2, 6)
                    .SetSpacing(-1)
                    .Bind(ref PlayerDataContainer),
                XUIVLayout.Make(
                        XUIIconButton.Make(AskForGuild)
                            .SetWidth(20)
                            .SetHeight(20)
                            .Bind(ref PlayerImage)
                    )
                    .SetPadding(2, 2, 2, 7)
                    .Bind(ref PlayerImageContainer),
                PagedLevelList.Make(_uiFactory, _modData)
                    .Bind(ref MainPlayerLevelsContainer)
                    .SetPadding(2, 2, 2, 12)
                    .SetSpacing(-0.5f)
                    .SetActive(false)
            )
            .OnReady(x => x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter)
            .OnReady(x => x.CSizeFitter.horizontalFit =
                x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained)
            .SetBackground(true)
            .SetBackgroundColor(Color.black.ColorWithAlpha(1))
            .Bind(ref MainLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
            )
            .SetBackground(true)
            .OnReady(x =>
            {
                //var l_Data = _resources.BorderMaterial;
                var sprite = _resources.BorderSprite;
                var material = _resources.BorderMaterial;
                var image = x.gameObject.GetComponent<ImageView>();
                image.material = material;
                image.sprite = sprite;
                _borderImage = image;
            })
            .OnReady(x => x.CSizeFitter.verticalFit =
                x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained)
            .BuildUI(transform);

        DontDestroyOnLoad(transform.gameObject);
        DontDestroyOnLoad(transform.parent.gameObject);

        _timeControl.EventChange += OnTimeChanged;
        Logic.OnSceneChange += OnSceneChanged;
        _cardFloatingScreen.HandleReleased += (ix, x) =>
        {
            if (Logic.ActiveScene != Logic.ESceneType.Playing)
                _config.PlayerCard.Transforms.Menu = new CardTransform(x.Position, x.Rotation);
            else
                _config.PlayerCard.Transforms.InSong = new CardTransform(x.Position, x.Rotation);
        };
    }

    protected override void OnViewDestruction()
    {
        Logic.OnSceneChange -= OnSceneChanged;
    }

    private void AskForGuild() => AskForGuild(false);

    private void AskForGuild(bool withWarning)
    {
        DisplayCard(EDisplayMode.Settings);

        GuildWarningMessageText.SetActive(withWarning);
        
        ShowSettingsButton.SetActive(!withWarning);

        _cardFloatingScreen.ScreenSize = new Vector2(50, 40);
    }

    public void OnTimeChanged(int hours, int minutes, int seconds)
        => TimeText.SetText($"{hours:00}:{minutes:00}:{seconds:00}");

    private void OnSceneChanged(Logic.ESceneType x)
    {
        switch (x)
        {
            case Logic.ESceneType.Menu:
                SetCardToMenuTransform();
                TimeText.SetActive(true);
                break;
            case Logic.ESceneType.Playing:
                SetCardToInSongTransform();
                TimeText.SetActive(false);
                break;
            default: return;
        }
    }

    private void EventGuildSelected(GuildResponses.GuildExtended? x)
    {
        if (_config.PlayerCard.GuildId == -1) return;

        if (x == null)
        {
            DisplayCard(EDisplayMode.Normal);
            LoadConfig();
            return;
        }

        _config.PlayerCard.GuildId = x.Guild.Id;
        _guildSaberManager.SelectGuild(x.Guild.Id, x.Contexts.First().Id);

        SetGuild(new GuildId(_config.PlayerCard.GuildId), UpdatePlayer);
    }

    public void RefreshCardSize(bool displayCardLevelsDetails)
    {
        if (_modData.PlayerLevels.Length == 0 && displayCardLevelsDetails)
        {
            RefreshCardSize(false);
            return;
        }

        float width = 55;
        if (displayCardLevelsDetails && _modData.PlayerLevels.Any())
            width += 30;

        if (_modData.Player != null)
            GetCardFloatingScreen().ScreenSize =
                new Vector2(width + _modData.Player.Player.PlayerInfo.Username.Length, 40);
    }

    public async void RefreshCard()
    {
        try
        {
            if (_modData.Player == null)
                return;

            var image = await TextureUtils.FetchImageFromUrl(_modData.Player.Player.PlayerInfo.AvatarUrl, _resources);
            if (image != null)
                PlayerImage.SetSprite(Sprite.Create(image, new Rect(0, 0, image.width, image.height), new Vector2()));

            DisplayCard(EDisplayMode.Normal);
            LoadConfig();
            UpdatePlayer();
        }
        catch (Exception e)
        {
            _logger.Error("[GuildSaberMod][RefreshCard] Error");
            _logger.Error(e);
        }
    }

    public void SetCardToMenuTransform()
    {
        GetCardFloatingScreen().transform.position = _config.PlayerCard.Transforms.Menu.Position;
        GetCardFloatingScreen().transform.rotation = _config.PlayerCard.Transforms.Menu.Rotation;
    }

    public void SetCardToInSongTransform()
    {
        GetCardFloatingScreen().transform.position = _config.PlayerCard.Transforms.InSong.Position;
        GetCardFloatingScreen().transform.rotation = _config.PlayerCard.Transforms.InSong.Rotation;
    }

    private void DisplaySettings()
    {
        if (!_cardSettingsCoordinator.IsPresent)
            _cardSettingsCoordinator.Present();

        DisplayCard(EDisplayMode.Normal);
    }

    private void ResetTimer()
    {
        _config.PlayerCard.TimeData.PlayDurationSec = 0;
        _timeControl.Reset();
        DisplayCard(EDisplayMode.Normal);
    }

    public void DisplayCard(EDisplayMode displayMode)
    {
        MainLayout.SetActive(displayMode == EDisplayMode.Normal);
        InvalidConfigLayout.SetActive(displayMode == EDisplayMode.Settings);
        LoadingLayout.SetActive(displayMode == EDisplayMode.Loading);
        ServerUnreachableLayout.SetActive(displayMode == EDisplayMode.Error);

        if (displayMode == EDisplayMode.Error)
        {
            return;
        }

        LoadConfig();

        GuildSelector.UpdateGuildButtons();
    }

    public void DisplayLevelsDetails(bool display)
    {
        if (_modData.PlayerLevels.Length == 0 && display)
        {
            DisplayLevelsDetails(false);
            return;
        }

        MainPlayerLevelsContainer.SetActive(display);
        RefreshCardSize(display);

        if (display)
        {
            PlayerDataContainer.SetPadding(2, 2, 2, 12);
            PlayerImageContainer.SetPadding(2, 2, 2, 12);
            MainPlayerLevelsContainer.SetPadding(2, 2, 2, 14);

            RefreshLevelsDetails();
        }
        else
        {
            PlayerDataContainer.SetPadding(2, 2, 2, 2);
            PlayerImageContainer.SetPadding(2, 2, 2, 2);
        }
    }

    public void RefreshLevelsDetails() => MainPlayerLevelsContainer.Refresh(_modData);

    public FloatingScreen GetCardFloatingScreen() => _cardFloatingScreen;

    public void SetGuild(GuildId guildId, Action? callback)
    {
        var guild = _modData.GetGuild(guildId);
        if (guild == null) return;

        RefreshCard();

        if (callback != null) callback.Invoke();
    }

    public void UpdatePlayer()
    {
        if (_modData.Player == null) return;

        var level = _modData.PlayerLevels
            .Where(x => x.Level.CategoryId is null && !x.IsLocked)
            .LastOrDefault(x => x.IsCompleted);

        PlayerNameText.SetText(_modData.Player.Player.PlayerInfo.Username);
        PlayerLevelText.SetText($"{level?.Level.Info.Name ?? "Level none"}");

        int passCount = 0;
        foreach (var memberLevelStat in _modData.PlayerLevels.Where(x => x.Level.CategoryId == null))
        {
            passCount += memberLevelStat?.PassCount ?? 0;
        }

        PlayerPassesText.SetText($"Pass count: {passCount}");


        PointsContainer.Refresh(_modData);
    }

    public void LoadConfig()
    {
        DisplayLevelsDetails(_config.PlayerCard.CategoryLevelViewEnabled);

        if (_modData.Player is null)
        {
            _logger.Error("[PlayerCard] Player is null, cannot set colors");
            return;
        }

        if (_config.PlayerCard.ColorSettings.UseCustomColors
            && PlayerCardLibrary.CanPlayerUseCustomColors(_modData.PlayerLevels, _modData.Player))
        {
            _borderImage.color = _config.PlayerCard.ColorSettings.MainCardColor;
            PlayerNameText.SetColor(_config.PlayerCard.ColorSettings.MainCardColor);

            _borderImage.gradient = _config.PlayerCard.ColorSettings.UseGradient;

            if (_config.PlayerCard.ColorSettings.UseGradient)
            {
                _borderImage.color = Color.white;
                _borderImage.color0 = _config.PlayerCard.ColorSettings.GradientColor0;
                _borderImage.color1 = _config.PlayerCard.ColorSettings.GradientColor1;
            }
            else
            {
                _borderImage.color0 = _config.PlayerCard.ColorSettings.MainCardColor;
                _borderImage.color1 = _config.PlayerCard.ColorSettings.MainCardColor;
            }

            return;
        }

        var level = _modData.PlayerLevels
            .Where(x => x.Level.CategoryId == null && !x.IsLocked)
            .LastOrDefault(x => x.IsCompleted);

        if (level == null)
        {
            _borderImage.color = Color.white;
            _borderImage.color0 = Color.white;
            _borderImage.color1 = Color.white;
            return;
        }

        var color = PlayerCardLibrary.FromArgb(level.Level.Info.Color);
        PlayerNameText.SetColor(color);
        _borderImage.color = color;
        _borderImage.color1 = color;
        _borderImage.color0 = color;
    }
}