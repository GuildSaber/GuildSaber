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
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.Core.UI.Utils;
using HMUI;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Color = UnityEngine.Color;

namespace GuildSaber.Mod.Core.PlayerCard.UI;

internal class PlayerCardView : ViewController<PlayerCardView>
{
    public enum EDisplayMode
    {
        Normal,
        Settings,
        Error
    }

    [Inject(Id = Constants.CardFloatingPanelId)] private readonly FloatingScreen _cardFloatingScreen = null!;
    [Inject] private readonly PlayerCardSettingsCoordinator _cardSettingsCoordinator = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly GuildSaberManager _guildSaberManager = null!;
    [Inject] private readonly SiraLog _logger = null!;
    [Inject] private readonly ModData _modData = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;
    [Inject] private readonly TimeController _timeControl = null!;

    private ImageView _borderImage = null!;

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

    protected PointList PointsContainer = null!;
    protected GSSecondaryButton ShowSettingsButton = null!;
    protected GSText TimeText = null!;

    protected override void OnViewCreation()
    {
        XUIVLayout.Make(
                GSText.Make("Please select a guild to use the Player Card")
                    .Bind(ref GuildWarningMessageText)
                    .SetColor(Color.yellow),
                GuildSelector.Make()
                    .SetOnGuildSelected(EventGuildSelected),
                GSSecondaryButton.Make("Show settings")
                    .Bind(ref ShowSettingsButton)
                    .SetWidth(20)
                    .SetHeight(5)
                    .OnClick(DisplaySettings),
                GSSecondaryButton.Make("Reset timer")
                    .SetWidth(20)
                    .SetHeight(5)
                    .OnClick(ResetTimer)
            )
            .Bind(ref InvalidConfigLayout)
            .BuildUI(transform);

        XUIVLayout.Make(
                //GSLoadingIndicator.Make()
                GSText.Make("Loading... TODO: Replace this text by the loading indicator of the base game")
            ).Bind(ref LoadingLayout)
            .BuildUI(transform);

        XUIHLayout.Make(
                XUIVLayout.Make(
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerNameText)
                            .SetStyle(FontStyles.Underline | FontStyles.Bold)
                            .SetFontSize(5),
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerPassesText)
                            .SetFontSize(3.7f),
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerLevelText)
                            .SetFontSize(4.5f),
                        PointList.Make()
                            .Bind(ref PointsContainer)
                            .SetSpacing(0),
                        GSText.Make("______")
                            .SetFontSize(3),
                        GSText.Make("00:00:00")
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
                PagedLevelList.Make()
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
                var l_Sprite = _resources.BorderSprite;
                var l_Material = _resources.BorderMaterial;
                var l_Image = x.gameObject.GetComponent<ImageView>();
                l_Image.material = l_Material;
                l_Image.sprite = l_Sprite;
                _borderImage = l_Image;
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

    private void AskForGuild() => AskForGuild(false);

    private void AskForGuild(bool withWarning)
    {
        GuildWarningMessageText.SetActive(withWarning);

        FastAnimator.Animate([
            new FastAnimator.FloatAnimKey(0, 0),
            new FastAnimator.FloatAnimKey(1, 0.2f)
        ], x => InvalidConfigLayout.Element.transform.localScale = new Vector3(x, x, 1));

        ShowSettingsButton.SetActive(!withWarning);

        _cardFloatingScreen.ScreenSize = new Vector2(50, 40);
    }

    public void OnTimeChanged(int hours, int minutes, int seconds)
        => TimeText.SetText($"{hours:00}:{minutes:00}:{seconds:00}");

    private void OnSceneChanged(Logic.ESceneType x)
    {
        if (x != Logic.ESceneType.Playing)
        {
            GetCardFloatingScreen().transform.position = _config.PlayerCard.Transforms.Menu.Position;
            GetCardFloatingScreen().transform.rotation = _config.PlayerCard.Transforms.Menu.Rotation;
        }
        else
        {
            GetCardFloatingScreen().transform.position = _config.PlayerCard.Transforms.InSong.Position;
            GetCardFloatingScreen().transform.rotation = _config.PlayerCard.Transforms.InSong.Rotation;
        }
    }

    private void EventGuildSelected(GuildResponses.Guild? x)
    {
        if (_config.PlayerCard.GuildId == -1) return;

        if (x == null)
        {
            DisplayCard(EDisplayMode.Normal);
            LoadConfig();
            return;
        }

        _config.PlayerCard.GuildId = x.Id.Value;

        _guildSaberManager.SelectGuild(x.Id, 0);

        SetGuild(new GuildId(_config.PlayerCard.GuildId), SetPlayer);
    }

    public void RefreshCardSize(bool displayCardLevelsDetails)
    {
        if (_modData.PlayerLevels.Length == 0 && displayCardLevelsDetails)
        {
            RefreshCardSize(false);
            return;
        }

        float l_Width = 55;
        if (displayCardLevelsDetails && _modData.PlayerLevels.Any())
            l_Width += 30;

        if (_modData.Player != null)
            GetCardFloatingScreen().ScreenSize =
                new Vector2(l_Width + _modData.Player.Player.PlayerInfo.Username.Length, 40);
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
        }
        catch (Exception e)
        {
            _logger.Error("[GuildSaberMod][RefreshCard] Error");
            _logger.Error(e);
        }
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

    public void DisplayCard(EDisplayMode displayMode) { }

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
        var l_Guild = _modData.GetGuild(guildId.Value);
        if (l_Guild == null) return;

        RefreshCard();

        if (callback != null) callback.Invoke();
    }

    public void SetPlayer()
    {
        var level = _modData.PlayerLevels
            .Where(x => x.Level.CategoryId == null && !x.IsLocked)
            .LastOrDefault(x => x.IsCompleted);

        PlayerNameText.SetText($"Level: {level?.Level.Info.Name ?? ""}");
        PlayerPassesText.SetText($"Pass count: {level?.PassCount ?? 0}");
        PointsContainer.Refresh(_modData);
    }

    public void LoadConfig()
    {
        DisplayLevelsDetails(_config.PlayerCard.CategoryLevelViewEnabled);

        if (_modData.Player is null)
            return;

        if (_config.PlayerCard.ColorSettings.UseCustomColors
            && PlayerCardLibrary.CanPlayerUseCustomColors(_modData.PlayerLevels, _modData.Player.Player))
        {
            _borderImage.color = _config.PlayerCard.ColorSettings.MainCardColor;

            if (_config.PlayerCard.ColorSettings.UseGradient)
            {
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
        _borderImage.color = color;
        _borderImage.color1 = color;
        _borderImage.color0 = color;
    }
}