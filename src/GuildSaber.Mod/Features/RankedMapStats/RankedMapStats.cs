using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK.XUI;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using GuildSaber.Mod.Resources;
using HMUI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.RankedMapStats;

public class RankedMapStats : XUIVLayout, IDisposable
{
    private readonly GuildSaberCache _cache;
    private readonly GuildSaberClient _client;
    private readonly GuildSaberConfig _config;
    private readonly StandardLevelDetailViewController _levelDetailViewController;
    private readonly GameplayModifiersPanelController _gameplayModifiersPanelController;
    private readonly Logger _logger;
    private readonly Texture2D _placeHolderIcon;
    private XUIImage _categoryIcon = null!;
    
    private XUIImage _guildIcon = null!;
    private GSText _mapCategories = null!;
    private GSText _mapLevel = null!;
    private XUIImage _whiteCheckMarkImage = null!;

    private RankedMapResponses.RankedMap? _rankedMap;
    private ImageView[] _actionButtonBaseImageViews;
    private ImageView[] _actionButtonRedImageViews;
    
    public RankedMapStats(
        [Inject] GuildSaberCache cache,
        [Inject] UIFactory factory,
        [Inject] GuildSaberConfig config,
        [Inject] GuildSaberClient client,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D placeHolderIcon,
        [Inject(Id = nameof(ResourceMap.WhiteCheckMark))] Texture2D whiteCheckMarkTexture,
        [Inject] StandardLevelDetailViewController levelDetailViewController,
        [Inject] GameplayModifiersPanelController gameplayModifiersPanelController,
        [Inject] Logger logger
    ) : base("MapRankedStats")
    {
        _cache = cache;
        _config = config;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _placeHolderIcon = placeHolderIcon;
        _levelDetailViewController = levelDetailViewController;
        _gameplayModifiersPanelController = gameplayModifiersPanelController;
        _logger = logger;
        
        var componentsInActionButton = _levelDetailViewController._standardLevelDetailView.actionButton.GetComponentsInChildren<ImageView>();

        _actionButtonBaseImageViews = componentsInActionButton;

        List<ImageView> redImageViews = new List<ImageView>();
        foreach (var imageView in componentsInActionButton)
        {
            var newImageViewGo = GameObject.Instantiate(imageView.gameObject, imageView.transform.parent, false);
            newImageViewGo.name = imageView.gameObject.name + "_red";
            var newImageView = newImageViewGo.GetComponent<ImageView>();

            newImageView.color = Color.white;
            newImageView.color0 = Color.red;
            newImageView.color1 = new Color(1.0f, 0.3f, 0.0f);
            newImageView.gameObject.SetActive(false);
            newImageView.gameObject.transform.SetSiblingIndex(imageView.gameObject.transform.GetSiblingIndex() + 1);
            redImageViews.Add(newImageView);
        }

        _actionButtonRedImageViews = redImageViews.ToArray();
    
        _logger.Info("Preparing UI");
        OnReady(x =>
        {
            XUIHLayout.Make(
                    XUIImage.Make()
                        .Bind(ref _guildIcon)
                        .SetActive(false)
                        .SetHeight(6)
                        .SetWidth(6),
                    factory.Text(string.Empty)
                        .Bind(ref _mapLevel)
                        .SetFontSize(5)
                        .SetAlpha(0.55f))
                .SetSpacing(2)
                .BuildUI(x.transform);

            XUIHLayout.Make(
                factory.Text(string.Empty)
                    .Bind(ref _mapCategories)
                    .SetFontSize(4)
                    .SetAlpha(0.55f)
                    .SetActive(false),
                XUIImage.Make()
                    .Bind(ref _categoryIcon)
                    .SetActive(false)
                    .SetHeight(8)
                    .SetWidth(8)
            ).BuildUI(x.transform);

            XUIImage.Make(Sprite.Create(whiteCheckMarkTexture,
                    new Rect(0, 0, whiteCheckMarkTexture.width, whiteCheckMarkTexture.height), Vector2.zero)
                ).Bind(ref _whiteCheckMarkImage)
                .SetWidth(3)
                .SetHeight(3)
                .SetActive(false)
                .OnReady(i => i.LElement.ignoreLayout = true)
                .BuildUI(x.transform);

            x.SetSpacing(2);
            x.VLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            x.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            _levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
            _levelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyChanged;
            _levelDetailViewController.didChangeContentEvent -= OnContentChanged;
            _levelDetailViewController.didChangeContentEvent += OnContentChanged;

            // Move the ranked stats UI to the right of the levelParamsPanel.
            RTransform.offsetMin = new Vector2(80, -5);

        });

        _gameplayModifiersPanelController.didChangeGameplayModifiersEvent += OnModifiersChanged;
        BuildUI(_levelDetailViewController._standardLevelDetailView._levelParamsPanel.transform);
        _logger.Info("UI created and attached to levelParamsPanel");
    }

    private void OnModifiersChanged()
    {
        UpdateLevelIsEligibleForValidation(_rankedMap);
    }

    public void UpdateLevelIsEligibleForValidation(RankedMapResponses.RankedMap? rankedMap)
    {
        GameplayModifiers gameplayModifiers = _gameplayModifiersPanelController.gameplayModifiers;
         
        RankedMapRequests.EModifiers modifiers = gameplayModifiers == null ? RankedMapRequests.EModifiers.None : GameplayModifiersToEnum(gameplayModifiers);

        bool isEligible = rankedMap != null 
                          && (((modifiers & rankedMap.Requirements.ProhibitedModifiers) == RankedMapRequests.EModifiers.None) 
                              && (modifiers & RankedMapRequests.EModifiers.Unk) == RankedMapRequests.EModifiers.None
                          && (modifiers & rankedMap.Requirements.MandatoryModifiers) == rankedMap.Requirements.MandatoryModifiers);
        
        foreach (var imageView in _actionButtonBaseImageViews)
        {
            imageView.gameObject.SetActive(isEligible);
        }
        
        foreach (var imageView in _actionButtonRedImageViews)
        {
            imageView.gameObject.SetActive(!isEligible);
        }
    }
    
    /// <remarks>
    /// As of SongCore v15.0.0, the `Hashing.GetCustomLevelHash` method got obsolete in favor of the new
    /// `Hashing.ComputeCustomLevelHash`.
    /// In case the obsolete `Hashing.GetCustomLevelHash` method is removed in future versions of SongCore,
    /// just replace the method name check in the LINQ query with it's string literal "GetCustomLevelHash"
    /// </remarks>
    [field: MaybeNull, AllowNull]
    private Func<BeatmapLevel, string> GetCustomHashMethodVersionAgnostic => field ??= typeof(Hashing).GetMethods()
        .Where(m => m.Name is "ComputeCustomLevelHash" or "GetCustomLevelHash" &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(BeatmapLevel))
        .OrderBy(m => m.Name == "ComputeCustomLevelHash")
        .First(m => m.ReturnType == typeof(string))
        .ToDelegate<Func<BeatmapLevel, string>>();

    public void Dispose()
    {
        _logger.Info("Disposing..");
        _levelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyChanged;
        _levelDetailViewController.didChangeContentEvent -= OnContentChanged;
    }

    private void OnDifficultyChanged(StandardLevelDetailViewController controller)
        => _ = UpdateUI(controller.beatmapKey, controller.beatmapLevel);

    private void OnContentChanged(
        StandardLevelDetailViewController controller, StandardLevelDetailViewController.ContentType contentType)
    {
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady) return;
        _ = UpdateUI(controller.beatmapKey, controller.beatmapLevel);
    }

    protected async Task UpdateUI(BeatmapKey beatmapKey, BeatmapLevel? beatmap)
    {
        if (!_config.RankedMapStats.Enabled || _cache.PlayerExtended == null || beatmap == null || !SongHash
                .TryCreate(GetCustomHashMethodVersionAgnostic.Invoke(beatmap))
                .TryGetValue(out var songHash))
        {
            SetActive(false);
            return;
        }

        var rankedMapWithScoresOfPlayer = await FetchRankedMapWithScoresOfPlayer(
            _config.ContextId,
            _cache.PlayerExtended!.Player.Id,
            songHash,
            beatmapKey.beatmapCharacteristic.serializedName,
            beatmapKey.difficulty.ToEDifficulty(),
            _client
        );

        try
        {
            UpdateLevelIsEligibleForValidation(rankedMapWithScoresOfPlayer?.RankedMap);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        if (rankedMapWithScoresOfPlayer is null)
        {
            SetActive(false);
            _rankedMap = null;
            return;
        }
        
        _rankedMap = rankedMapWithScoresOfPlayer.RankedMap;

        var guildIconTexture = await _cache.FetchGuildIconTexture(_config.GuildId, _client) ?? _placeHolderIcon;
        var roundedIcon = await TextureUtils.CreateRoundedTextureAsync(guildIconTexture, guildIconTexture.width * 0.2f);

        if (rankedMapWithScoresOfPlayer.RankedMap.CategoryIds.Length == 0)
        {
            _categoryIcon.SetActive(false);
            _mapCategories.SetActive(false);
        }
        else
        {
            var categories = _cache.GuildsExtended[_config.GuildId].Categories
                .Where(x => rankedMapWithScoresOfPlayer.RankedMap.CategoryIds.Contains(x.Id)).ToArray();

            var hasCategoryIcon = false;
            if (categories.Length == 1)
            {
                var categoryIconTexture = await _cache.FetchCategoryIconTexture(categories[0].Id, _client);
                if (categoryIconTexture != null)
                {
                    var roundedCategoryIcon = await TextureUtils.CreateRoundedTextureAsync(
                        categoryIconTexture, categoryIconTexture.width * 0.2f);
                    _categoryIcon.SetSprite(Sprite.Create(
                        roundedCategoryIcon,
                        new Rect(0, 0, roundedCategoryIcon.width, roundedCategoryIcon.height),
                        Vector2.zero));

                    hasCategoryIcon = true;
                }
            }

            if (!hasCategoryIcon)
            {
                var categoryNames = string.Join("\n", categories.Select(x => x.Info.Name));

                _mapCategories.SetText(categoryNames);
                _mapCategories.SetActive(!string.IsNullOrEmpty(categoryNames));
                _categoryIcon.SetActive(false);
            }
            else
            {
                _mapCategories.SetActive(false);
                _categoryIcon.SetActive(true);
            }
        }

        _mapLevel.SetText($"{(int)rankedMapWithScoresOfPlayer.RankedMap.Rating.DiffStar}");
        _guildIcon.SetSprite(Sprite.Create(roundedIcon, new Rect(0, 0, roundedIcon.width, roundedIcon.height),
            Vector2.zero));
        _guildIcon.SetActive(true);

        var playerPassedTheMap = rankedMapWithScoresOfPlayer
            .RankedScores
            .Any(x => x.State.HasFlag(RankedScoreResponses.EState.Selected)
                      && (x.State & RankedScoreResponses.EState.NonPointGiving) == 0);

        _whiteCheckMarkImage.SetActive(playerPassedTheMap);

        SetActive(true);
    }

    public RankedMapRequests.EModifiers GameplayModifiersToEnum(GameplayModifiers gameplayModifiers)
    {
        var modifiers = RankedMapRequests.EModifiers.None;

        if (gameplayModifiers.disappearingArrows) modifiers |= RankedMapRequests.EModifiers.DisappearingArrows;
        if (gameplayModifiers.ghostNotes) modifiers |= RankedMapRequests.EModifiers.GhostNotes;
        if (gameplayModifiers.noArrows) modifiers |= RankedMapRequests.EModifiers.NoArrows;
        if (gameplayModifiers.enabledObstacleType != GameplayModifiers.EnabledObstacleType.All)
            modifiers |= RankedMapRequests.EModifiers.NoObstacles; 
        if (gameplayModifiers.noBombs) modifiers |= RankedMapRequests.EModifiers.NoBombs;
        if (gameplayModifiers.zenMode) modifiers |= RankedMapRequests.EModifiers.Unk;
        if (gameplayModifiers.proMode) modifiers |= RankedMapRequests.EModifiers.ProMode;
        if (gameplayModifiers.strictAngles) modifiers |= RankedMapRequests.EModifiers.StrictAngles;
        if (gameplayModifiers.smallCubes) modifiers |= RankedMapRequests.EModifiers.SmallNotes;
        if (gameplayModifiers.songSpeedMul < 1.0f) modifiers |= RankedMapRequests.EModifiers.SlowerSong;
        if (gameplayModifiers.instaFail) modifiers |= RankedMapRequests.EModifiers.InstaFail;
        if (gameplayModifiers.failOnSaberClash) modifiers |= RankedMapRequests.EModifiers.BatteryEnergy;
        
        return modifiers;
    }
    
    public async Task<RankedMapResponses.RankedMapWithScores?> FetchRankedMapWithScoresOfPlayer(
        ContextId contextId, PlayerId playerId, SongHash hash, string mode, EDifficulty difficulty,
        GuildSaberClient client)
        => (await _cache.FetchRankedMaps(contextId, playerId, hash, client))
            .FirstOrDefault(x => x.RankedMap.Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty));

    public sealed override void BuildUI(Transform parent) => base.BuildUI(parent);
}