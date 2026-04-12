using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using CP_SDK_BS.Game;
using CP_SDK_BS.UI;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core;
using GuildSaber.Mod.Core.UI.Common;
using GuildSaber.Mod.Core.UI.Extensions;
using GuildSaber.Mod.PlayerCard.UI.Components;
using HMUI;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.PlayerCard.UI;

internal class PlayerCardView : ViewController<PlayerCardView>
{
    [Inject] private readonly SiraLog _logger = null!;
    [Inject] private readonly PlayerCardResources _resources = null!;
    [Inject] private readonly GSConfig Config = null!;
    [Inject] private readonly TimeController TimeControl = null!;
    [Inject] private readonly FloatingScreen CardFloatingScreen = null!;
    [Inject] private readonly ModData _modData = null!;
    
    private ImageView BorderImage = null!;
    
    protected GSText MessageText = null!;
    protected GSSecondaryButton ShowSettingsButton = null!;
    protected XUIVLayout InvalidConfigLayout = null!;
    protected XUIVLayout LoadingLayout = null!;

    protected GSText PlayerNameText = null!;
    protected GSText PlayerPassesText = null!;
    protected GSText PlayerLevelText = null!;
    protected GSText TimeText = null!;
    
    protected XUIVLayout PointsContainer = null!;
    protected XUIVLayout PlayerDataContainer = null!;
    
    protected XUIVLayout PlayerImageContainer = null!;

    protected XUIIconButton PlayerImage = null!;

    protected XUIGLayout PlayerLevelsContainer = null!;

    protected XUIHLayout MainLayout = null!;

    protected XUIVLayout MainPlayerLevelsContainer = null!;

    protected GSSecondaryButton PageLeftButton = null!;
    protected GSSecondaryButton PageRightButton = null!;
    
    
    
    protected override void OnViewCreation()
    {
        XUIVLayout.Make(
                GSText.Make("Please select a guild to use the Player Card")
                    .Bind(ref MessageText)
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
                GSLoadingIndicator.Make()
            ).Bind(ref LoadingLayout)
            .BuildUI(transform);

        XUIHLayout.Make(
                XUIVLayout.Make(
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerNameText)
                            .SetStyle(TMPro.FontStyles.Underline | TMPro.FontStyles.Bold)
                            .SetFontSize(5),
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerPassesText)
                            .SetFontSize(3.7f),
                        GSText.Make(string.Empty)
                            .Bind(ref PlayerLevelText)
                            .SetFontSize(4.5f),
                        XUIVLayout.Make()
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
                XUIVLayout.Make(
                        XUIGLayout.Make().Bind(ref PlayerLevelsContainer).SetSpacing(new Vector2(0, -1)).SetMinWidth(30)
                            .SetCellSize(new Vector2(18, 12f)).SetConstraintCount(2),
                        XUIHLayout.Make(
                                GSSecondaryButton.Make("<", PageLeft).SetWidth(5).SetHeight(5).Bind(ref PageLeftButton),
                                GSSecondaryButton.Make(">", PageRight).SetWidth(5).SetHeight(5)
                                    .Bind(ref PageRightButton)
                            ).SetSpacing(10)
                            .SetPadding(new RectOffset(-5, 2, 2, 2))
                    )
                    .SetPadding(2, 2, 2, 12)
                    .SetSpacing(-0.5f)
                    .SetActive(false)
                    .Bind(ref MainPlayerLevelsContainer)
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
                BorderImage = l_Image;
            })
            .OnReady(x => x.CSizeFitter.verticalFit =
                x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained)
            .BuildUI(transform);

        GameObject.DontDestroyOnLoad(transform.gameObject);
        GameObject.DontDestroyOnLoad(transform.parent.gameObject);

        TimeControl.EventChange += OnTimeChanged;
        Logic.OnSceneChange += OnSceneChanged;
        CardFloatingScreen.HandleReleased += (ix, x) =>
        {
            if (Logic.ActiveScene != Logic.ESceneType.Playing)
            {
                Config.PlayerCard.InMenuPosition = x.Position;
                Config.PlayerCard.InMenuRotation = x.Rotation.eulerAngles;
                Config.Save();
            }
            else
            {
                Config.PlayerCard.InSongPosition = x.Position;
                Config.PlayerCard.InSongRotation = x.Rotation.eulerAngles;
                Config.Save();
            }
        };
    }

    private void AskForGuild()
    {
        
    }
    
    public void OnTimeChanged(int hours, int minutes, int seconds)
    {
        TimeText.SetText($"{hours:00}:{minutes:00}:{seconds:00}");
    }

    private void OnSceneChanged(CP_SDK_BS.Game.Logic.ESceneType x)
    {
        if (x != Logic.ESceneType.Playing)
        {
            GetFloatingScreen().transform.position = GSConfig.Instance.CardInMenuPosition;
            GetFloatingScreen().transform.rotation = Quaternion.Euler(GSConfig.Instance.CardInMenuRotation);
        } else
        {
            GetFloatingScreen().transform.position = GSConfig.Instance.CardInSongPosition;
            GetFloatingScreen().transform.rotation = Quaternion.Euler(GSConfig.Instance.CardInSongRotation);
        }
    }

    private void EventGuildSelected(GuildResponses.Guild x)
    {
        if (x == null && Config.PlayerCard.GuildId == -1)
        {
            return;
        }

        if (x == null)
        {
            DisplayCard(EDisplayMode.Normal);
            LoadConfig();
            return;
        }

        Config.PlayerCard.GuildId = (int)x.Id.Value;
        Config.Save();

        SetGuild(Config.Card.GuildId, () => { SetPlayer(_modData.Player); });
    }

    public void RefreshCardSize(bool displayCardLevelsDetails)
            {
                if (CardPlayerData.CategoryData == null && displayCardLevelsDetails)
                {
                    RefreshCardSize(false);
                    return;
                }

                if (CardPlayerData.CategoryData.Count() == 0 && displayCardLevelsDetails)
                {
                    RefreshCardSize(false);
                    return;
                }

                float l_Width = 55;
                if (displayCardLevelsDetails && CardPlayerData.CategoryData.Any())
                    l_Width += 30;

                GSModule.CardFloatingPanel.ScreenSize =
                    new Vector2(l_Width + CardPlayerData.Name.Length, 40);
            }

            public async void RefreshCard()
            {
                LevelsPage = 0;

                PlayerNameText.SetText(CardPlayerData.Name);

                try {
                    PlayerLevelText.SetText($"LVL: {CardPlayerData.LevelValue.ToString()}");
                    PlayerPasses.SetText($"{CardPlayerData.GuildValidPassCount} passes");
                } catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                var l_ImageResult = await GuildSaberUtils.GetImage(CardPlayerData.Avatar ?? string.Empty);
                if (!l_ImageResult.IsError)
                    PlayerImage.SetSprite(Sprite.Create(l_ImageResult.Texture, new Rect(0, 0, l_ImageResult.Texture.width, l_ImageResult.Texture.height), new Vector2()));

                DisplayCard(EDisplayMode.Normal);

                LoadConfig();
            }

            public void RefreshPoints()
            {
                var l_Points = CardPlayerData.RankData;

                if (l_Points == null)
                {
                    l_Points = new List<PointsData>();
                }
                //Logger.Instance.Info(l_Points.Count.ToString());
                foreach (var l_Item in CardPoints)
                    l_Item.SetActive(false);

                for (int l_i = 0; l_i < l_Points.Count();l_i++)
                {
                    if (CardPoints.Count - 1 < l_i)
                    {
                        CardPoints l_Point = Components.CardPoints.Make();
                        l_Point.BuildUI(PointsContainer.Element.transform);
                        CardPoints.Add(l_Point);
                    }

                    CardPoints[l_i].SetPoints(l_Points.ElementAt(l_i), GetUsedColor());
                    CardPoints[l_i].SetActive(true);
                }
            }
    
    private void DisplaySettings()
    {
        if (PlayerCardSettingsFlowCoordinator.Instance == null)
        {
            PlayerCardSettingsFlowCoordinator.Instance =
                BeatSaberUI.CreateFlowCoordinator<PlayerCardSettingsFlowCoordinator>();
        }

        if (!PlayerCardSettingsFlowCoordinator.Instance.IsPresent)
            PlayerCardSettingsFlowCoordinator.Instance.Present();
        DisplayCard(EDisplayMode.Normal);
    }

    private void ResetTimer()
    {
        Config.PlayerCard.TimeData.PlayDurationSeconds = 0;
        TimeControl.Reset();
        DisplayCard(EDisplayMode.Normal);
    }
    
    private int GetMaxPage()
    {
        float l_PreciseValue = ((float)CardPlayerData.CategoryData.Count / LEVELS_COUNT_BY_PAGE);
        int l_Value = (int)l_PreciseValue;
        if (l_Value == l_PreciseValue)
        {
            l_Value -= 1;
        }

        return l_Value;
    }

    private void PageLeft()
    {
        if (LevelsPage == 0) return;

        LevelsPage -= 1;

        RefreshLevelsDetails();
    }

    private void PageRight()
    {
        if (LevelsPage == GetMaxPage()) return;

        LevelsPage += 1;

        RefreshLevelsDetails();
    }
    
    public void DisplayLevelsDetails(bool display)
    {
        if (CardPlayerData.CategoryData == null && display)
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
        } else
        {
            PlayerDataContainer.SetPadding(2, 2, 2, 2);
            PlayerImageContainer.SetPadding(2, 2, 2, 2);
        }
    }

    public void HideAllLevels()
    {
        foreach (var l_Item in CardLevels)
        {
            l_Item.SetText(string.Empty);
        }
    }
    }
    
    public void RefreshLevelsDetails()
            {
                if (CardPlayerData.CategoryData.Count == 0)
                {
                    Logger.Instance.Info("No levels ???");
                    DisplayLevelsDetails(false);
                    return;
                }

                if (CardLevels.Count == 0)
                {
                    for (int l_i = 0; l_i < LEVELS_COUNT_BY_PAGE;l_i++)
                    {
                        CardLevel l_Level = CardLevel.Make();
                        l_Level.BuildUI(PlayerLevelsContainer.Element.transform);
                        CardLevels.Add(l_Level);
                    }
                }

                HideAllLevels();

                int l_Page = LevelsPage;

                var l_AllCategories = CardPlayerData.CategoryData;
                List<CategoryData> l_DisplayedCategories = new List<CategoryData>();
                for (int l_i = 0; l_i < l_AllCategories.Count;l_i++)
                {
                    if (l_i >= LEVELS_COUNT_BY_PAGE * l_Page && l_i < LEVELS_COUNT_BY_PAGE * (l_Page + 1))
                    {
                        l_DisplayedCategories.Add(l_AllCategories.ElementAt(l_i));
                    }
                }

                for (int l_i = 0; l_i < l_DisplayedCategories.Count();l_i++)
                {

                    //CardLevels[l_i].SetActive(true);
                    var l_Category = l_DisplayedCategories[l_i];
                    if (l_Category == null)
                    {
                        Logger.Instance.Error($"Could not get category with id {l_DisplayedCategories[l_i].CategoryID}");
                        CardLevels[l_i].SetActive(false);
                        continue;
                    }
                    CardLevels[l_i].SetLevel(l_DisplayedCategories[l_i].CategoryName, l_DisplayedCategories[l_i].LevelValue);
                }

                PageLeftButton.SetActive(CardPlayerData.CategoryData.Count > LEVELS_COUNT_BY_PAGE);
                PageRightButton.SetActive(CardPlayerData.CategoryData.Count > LEVELS_COUNT_BY_PAGE);
                
            }
}