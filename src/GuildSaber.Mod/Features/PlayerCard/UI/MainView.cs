using System;
using System.Diagnostics;
using System.Linq;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.Game;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Http;
using GuildSaber.CSharpClient;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.Mod.Features.Common.Timer;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Caching;
using GuildSaber.Mod.Features.GuildSaber.Runtime;
using GuildSaber.Mod.Features.PlayerCard.UI.Components;
using GuildSaber.Mod.Features.PlayerCard.UI.Components.GuildSelector;
using GuildSaber.Mod.Features.PlayerCard.UI.Settings;
using GuildSaber.Mod.Features.PlaylistDownloader.UI;
using GuildSaber.Mod.Helpers;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Color = UnityEngine.Color;

namespace GuildSaber.Mod.Features.PlayerCard.UI;

public class PlayerCardView : ViewController<PlayerCardView>
{
    public enum EDisplayMode
    {
        Normal,
        Settings,
        Loading,
        Error,
        DidntJoinGuild
    }

    [Inject(Id = Constants.CardFloatingPanelId)]
    private readonly FloatingScreen _cardFloatingScreen = null!;

    [Inject] private readonly PlayerCardSettingsCoordinator _cardSettingsCoordinator = null!;
    [Inject] private readonly GuildSaberClient _client = null!;
    [Inject] private readonly GuildSaberConfig _config = null!;
    [Inject] private readonly GuildAssetCache _guildAssetCache = null!;

    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    [Inject] private readonly GuildSelectorFlowCoordinator _guildSelectorFlowCoordinator = null!;
    [Inject] private readonly Logger _logger = null!;
    [Inject] private readonly PlaylistDownloaderCoordinator _playlistDownloaderCoordinator = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;
    [Inject] private readonly GuildSaberSession _session = null!;
    [Inject] private readonly Timer _timer = null!;
    [Inject] private readonly UIFactory _uiFactory = null!;

    private ImageView _borderImage = null!;

    protected XUIDropdown ContextDropdown = null!;
    protected XUIVLayout DidntJoinGuildLayout = null!;

    protected GuildSelector GuildSelector = null!;

    protected GSText GuildWarningMessageText = null!;
    protected XUIVLayout InvalidConfigLayout = null!;
    protected XUIVLayout LoadingLayout = null!;

    protected XUIHLayout MainLayout = null!;

    protected PagedLevelList MainPlayerLevelsContainer = null!;
    protected XUIVLayout PlayerDataContainer = null!;

    protected XUIIconButton PlayerImage = null!;

    protected XUIVLayout PlayerImageContainer = null!;
    protected GSText PlayerLevelText = null!;

    protected GSText PlayerNameText = null!;
    protected GSText PlayerPassesText = null!;
    protected TrophyList PlayerTrophyList = null!;

    protected PointList PointsContainer = null!;
    protected XUIVLayout ServerUnreachableLayout = null!;
    protected GSSecondaryButton ShowSettingsButton = null!;
    protected GSText TimeText = null!;

    protected override void OnViewCreation()
    {
        XUIVLayout.Make(
                _uiFactory.Text("Please select a guild to use the Player Card")
                    .Bind(ref GuildWarningMessageText)
                    .SetColor(Color.yellow),
                GuildSelector.Make(
                        guildSelectorFlowCoordinator: _guildSelectorFlowCoordinator,
                        session: _session,
                        guildAssetCache: _guildAssetCache,
                        downArrowTexture: _resources.DownArrowTexture,
                        whiteArrowTexture: _resources.GsWhiteLogoTexture)
                    .Bind(ref GuildSelector)
                    .SetOnGuildSelected(EventGuildSelected),
                XUIHLayout.Make(
                    _uiFactory.SecondaryButton("Show settings")
                        .Bind(ref ShowSettingsButton)
                        .SetWidth(20)
                        .SetHeight(5)
                        .OnClick(DisplaySettings),
                    _uiFactory.SecondaryButton("Playlists")
                        .SetWidth(20)
                        .SetHeight(5)
                        .OnClick(OpenPlaylistsDownloader)
                ),
                _uiFactory.Dropdown()
                    .Bind(ref ContextDropdown)
                    .OnValueChanged(ContextSelected)
            )
            .Bind(ref InvalidConfigLayout)
            .BuildUI(transform);

        ModalContainerRTransform.localScale *= 0.6f;
        XUIVLayout.Make(
                _uiFactory.Text(
                        "Server unreachable,\nor you're not registered on the website.\nYou will need to restard your game.")
                    .SetColor(new Color(1, 0.5f, 0)),
                _uiFactory.SecondaryButton("Open in browser")
                    .SetWidth(40)
                    .SetHeight(4)
                    .OnClick(() =>
                        Process.Start(_config.ApiEnv.ToWebsiteUri().ToString())
                    )
            )
            .Bind(ref ServerUnreachableLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
                _uiFactory.Text(
                        "You didn't join any guild yet,\nplease join a guild to use this mod.\nYou will need to restart your game.")
                    .SetColor(new Color(1, 0.5f, 0)),
                _uiFactory.SecondaryButton("Open in browser")
                    .SetWidth(40)
                    .SetHeight(4)
                    .OnClick(() =>
                        Process.Start(_config.ApiEnv.ToWebsiteUri().ToString())
                    )
            )
            .Bind(ref DidntJoinGuildLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
                _uiFactory.LoadingIndicator()
            )
            .Bind(ref LoadingLayout)
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
                        PointList.Make(_session, _config, _uiFactory)
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
                PagedLevelList.Make(_uiFactory, _session)
                    .Bind(ref MainPlayerLevelsContainer)
                    .SetPadding(2, 2, 2, 12)
                    .SetSpacing(-0.5f)
                    .SetActive(false),
                TrophyList.Make(_resources, _uiFactory)
                    .Bind(ref PlayerTrophyList)
                    .SetPadding(2, 2, 2, -5)
            )
            .OnReady(x =>
            {
                x.HOrVLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                x.CSizeFitter.horizontalFit = x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            })
            .SetBackground(true)
            .SetBackgroundColor(Color.black.WithAlpha(1))
            .Bind(ref MainLayout)
            .BuildUI(transform);

        XUIVLayout.Make()
            .SetBackground(true)
            .OnReady(x =>
            {
                var sprite = _resources.BorderSprite;
                var material = _resources.BorderMaterial;
                var image = x.gameObject.GetComponent<ImageView>();
                image.material = material;
                image.sprite = sprite;
                _borderImage = image;
            })
            .OnReady(x =>
                x.CSizeFitter.verticalFit = x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained)
            .BuildUI(transform);

        // So the component persists when scene changes.
        DontDestroyOnLoad(transform.parent.gameObject);

        OnTimeInit();

        _timer.OnTimeUpdate += OnTimerChanged;
        Logic.OnSceneChange += OnSceneChanged;
        _cardFloatingScreen.HandleReleased += (_, x) =>
        {
            if (Logic.ActiveScene != Logic.ESceneType.Playing)
                _config.PlayerCard.Transforms.Menu = new CardTransform(x.Position, x.Rotation);
            else
                _config.PlayerCard.Transforms.InSong = new CardTransform(x.Position, x.Rotation);
        };
    }

    protected override void OnViewDestruction() => Logic.OnSceneChange -= OnSceneChanged;

    private void AskForGuild() => AskForGuild(false);

    private void AskForGuild(bool withWarning)
    {
        DisplayCard(EDisplayMode.Settings);

        GuildWarningMessageText.SetActive(withWarning);

        ShowSettingsButton.SetActive(!withWarning);

        _cardFloatingScreen.ScreenSize = new Vector2(50, 40);
    }

    /// <summary>
    /// Sets the current day and restores the play duration if the day is the same as the saved one.
    /// </summary>
    public void OnTimeInit()
    {
        var currentTime = DateTime.Now;
        var savedTime = _config.PlayerCard.TimerConfig;

        if (savedTime.Day == currentTime.Day)
        {
            _timer.SetAddedTime(new Timer.TimeOnlyLite(0, 0, (int)savedTime.PlayDurationSec));
            return;
        }

        savedTime.Day = currentTime.Day;
        savedTime.PlayDurationSec = 0;
    }

    public void OnTimerChanged(Timer.TimeOnlyLite time)
    {
        TimeText.SetText($"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}");
        if (time.Seconds % 20 != 0) return;

        _config.PlayerCard.TimerConfig.PlayDurationSec += 20;
    }

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
            case Logic.ESceneType.None:
            default: return;
        }
    }

    private void EventGuildSelected(GuildResponses.GuildExtended? guildExtended)
    {
        if (_config.GuildId == -1)
            return;

        if (guildExtended == null)
        {
            DisplayCard(EDisplayMode.Normal);
            LoadConfig();
            return;
        }

        _guildSaberManager.SetGuild(guildExtended);
    }

    private void ContextSelected(int index, string _)
    {
        if (!_guildSaberManager.Initialized)
            return;

        if (index < 0 || index >= _session.CurrentGuild.Contexts.Length)
            return;

        var contextId = _session.CurrentGuild.Contexts[index].Id;

        _guildSaberManager.SelectGuild(_config.GuildId, contextId);
    }

    public void RefreshCardSize(bool displayCardLevelsDetails)
    {
        while (true)
        {
            if (!_session.TryGetCurrentMemberLevelStats(out var memberLevelStats))
                return;

            if (memberLevelStats.Length == 0 && displayCardLevelsDetails)
            {
                displayCardLevelsDetails = false;
                continue;
            }

            float width = 55 + 8;
            if (displayCardLevelsDetails && memberLevelStats.Length > 0)
                width += 30;

            GetCardFloatingScreen().ScreenSize = new Vector2(
                width + _session.PlayerExtended.Player.PlayerInfo.Username.Length,
                40
            );

            break;
        }
    }

    public async void RefreshCard()
    {
        try
        {
            if (!_guildSaberManager.Initialized)
            {
                _logger.Error("[PlayerCard/RefreshCard]: GuildSaber is not initialized.");
                return;
            }

            var texture = new Texture2D(100, 100);

            try
            {
                var bytes = await _client.HttpClient.GetByteArrayAsync(
                    _session.PlayerExtended.Player.PlayerInfo.AvatarUrl);
                texture.LoadImage(bytes);
            }
            catch
            {
                texture = _resources.GsWhiteLogoTexture;
            }

            PlayerImage.SetSprite(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2()));
            var currentGuild = _session.CurrentGuild;
            var contextNames = currentGuild.Contexts.Select(t => t.Info.Name).ToList();
            var currentContextIndex = Array.FindIndex(currentGuild.Contexts, x => x.Id == _config.ContextId);
            var currentContextName = currentContextIndex >= 0
                ? contextNames[currentContextIndex]
                : contextNames[0];

            ContextDropdown.SetOptions(contextNames);
            ContextDropdown.SetValue(currentContextName, false);

            LoadConfig();
            DisplayCard(EDisplayMode.Normal);
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
        _cardSettingsCoordinator.Present();
        DisplayCard(EDisplayMode.Normal);
    }

    private void OpenPlaylistsDownloader()
    {
        _playlistDownloaderCoordinator.Present();
        DisplayCard(EDisplayMode.Normal);
    }

    public void DisplayCard(EDisplayMode displayMode)
    {
        MainLayout.SetActive(displayMode == EDisplayMode.Normal);
        InvalidConfigLayout.SetActive(displayMode == EDisplayMode.Settings);
        LoadingLayout.SetActive(displayMode == EDisplayMode.Loading);
        ServerUnreachableLayout.SetActive(displayMode == EDisplayMode.Error);
        DidntJoinGuildLayout.SetActive(displayMode == EDisplayMode.DidntJoinGuild);

        if (displayMode is EDisplayMode.Error or EDisplayMode.Loading) return;

        LoadConfig();

        GuildSelector.UpdateGuildButtons();
    }

    public void DisplayLevelsDetails(bool display)
    {
        if (!_session.HasCurrentMemberStats && display)
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

    public void RefreshLevelsDetails() => MainPlayerLevelsContainer.CleanRefresh();

    public FloatingScreen GetCardFloatingScreen() => _cardFloatingScreen;

    public void RefreshMemberStats()
    {
        if (!_session.HasCurrentMemberStats) return;

        LoadConfig();
        UpdatePlayer();
    }

    public void UpdatePlayer()
    {
        if (!_guildSaberManager.Initialized) return;

        PointsContainer.Refresh();

        var memberLevelStats = _session.CurrentMemberLevelStats;
        var level = memberLevelStats.GetGlobalLevel();

        PlayerNameText.SetText(_session.PlayerExtended.Player.PlayerInfo.Username);
        PlayerLevelText.SetText($"{level?.Info.Name ?? "Level none"}");
        PlayerTrophyList.Refresh(memberLevelStats.CalculateTrophiesData());

        var globalPassStat = _session.CurrentMemberContextStats
            .PassCountsWithRank
            .FirstOrDefault(x => x.CategoryId is null);

        PlayerPassesText.SetText($"Pass count: {globalPassStat.PassCount} (#{globalPassStat.Rank})");
    }

    public void LoadConfig()
    {
        DisplayLevelsDetails(_config.PlayerCard.CategoryLevelViewEnabled);

        if (!_guildSaberManager.Initialized || !_session.HasCurrentMemberStats)
        {
            _logger.Error("[PlayerCard] GuildSaber is not initialized, cannot set colors");
            return;
        }

        if (_config.PlayerCard.ColorSettings.UseCustomColors
            && PlayerCardLibrary.CanPlayerUseCustomColors(
                _session.CurrentMemberLevelStats,
                _session.PlayerExtended.Player))
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

            PointsContainer.Refresh();
            return;
        }

        var level = _session.CurrentMemberLevelStats.GetGlobalLevel();
        if (level == null)
        {
            _borderImage.color = Color.white;
            _borderImage.color0 = Color.white;
            _borderImage.color1 = Color.white;
            return;
        }

        var color = PlayerCardLibrary.FromArgb(level.Info.Color);
        PlayerNameText.SetColor(color);
        _borderImage.color = color;
        _borderImage.color1 = color;
        _borderImage.color0 = color;

        PointsContainer.Refresh();
    }
}