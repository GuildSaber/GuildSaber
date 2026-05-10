using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK.XUI;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using GuildSaber.Mod.Resources;
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
    private readonly Logger _logger;
    private readonly Texture2D _placeHolderIcon;
    private XUIImage _categoryIcon = null!;

    private XUIImage _guildIcon = null!;
    private GSText _mapCategories = null!;
    private GSText _mapLevel = null!;

    public RankedMapStats(
        [Inject] GuildSaberCache cache,
        [Inject] UIFactory factory,
        [Inject] GuildSaberConfig config,
        [Inject] GuildSaberClient client,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D placeHolderIcon,
        [Inject] StandardLevelDetailViewController levelDetailViewController,
        [Inject] Logger logger
    ) : base("MapRankedStats")
    {
        _cache = cache;
        _config = config;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _placeHolderIcon = placeHolderIcon;
        _levelDetailViewController = levelDetailViewController;
        _logger = logger;

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

        BuildUI(_levelDetailViewController._standardLevelDetailView._levelParamsPanel.transform);
        _logger.Info("UI created and attached to levelParamsPanel");
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
        _logger.Info($"Updating ranked map stats for beatmap {beatmapKey}");
        if (!_config.RankedMapStats.Enabled || beatmap == null || !SongHash
                .TryCreate(GetCustomHashMethodVersionAgnostic.Invoke(beatmap))
                .TryGetValue(out var songHash))
        {
            SetActive(false);
            return;
        }

        var rankedMap = await FetchRankedMap(
            _config.ContextId,
            songHash,
            beatmapKey.beatmapCharacteristic.serializedName,
            beatmapKey.difficulty.ToEDifficulty(),
            _client
        );

        if (rankedMap is null)
        {
            SetActive(false);
            return;
        }

        var guildIconTexture = await _cache.FetchGuildIconTexture(_config.GuildId, _client) ?? _placeHolderIcon;
        var roundedIcon = await TextureUtils.CreateRoundedTextureAsync(guildIconTexture, guildIconTexture.width * 0.2f);

        if (rankedMap.CategoryIds.Length == 0)
        {
            _categoryIcon.SetActive(false);
            _mapCategories.SetActive(false);
        }
        else
        {
            var categories = _cache.GuildsExtended[_config.GuildId].Categories
                .Where(x => rankedMap.CategoryIds.Contains(x.Id)).ToArray();

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

        _mapLevel.SetText($"{(int)rankedMap.Rating.DiffStar}");
        _guildIcon.SetSprite(Sprite.Create(roundedIcon, new Rect(0, 0, roundedIcon.width, roundedIcon.height),
            Vector2.zero));
        _guildIcon.SetActive(true);

        SetActive(true);
    }

    public async Task<RankedMapResponses.RankedMap?> FetchRankedMap(
        ContextId contextId, SongHash hash, string mode, EDifficulty difficulty, GuildSaberClient client)
        => (await _cache.FetchRankedMaps(contextId, hash, client))
            .FirstOrDefault(x => x.Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty));

    public sealed override void BuildUI(Transform parent) => base.BuildUI(parent);
}