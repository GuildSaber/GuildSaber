using System;
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

public class MapRankedStats : XUIVLayout
{
    private readonly GuildSaberClient _client;
    private readonly GuildSaberConfig _config;
    private readonly Texture2D _gsWhiteLogoTexture;
    private readonly GuildSaberCache _guildSaberCache;

    private BeatmapLevel? _beatmapLevel;
    private XUIImage _categoryIcon = null!;

    private XUIImage _guildIcon = null!;
    private GSText _mapCategories = null!;
    private GSText _mapLevel = null!;

    public MapRankedStats(
        [Inject] GuildSaberCache guildSaberCache,
        [Inject] UIFactory factory,
        [Inject] GuildSaberConfig config,
        [Inject] GuildSaberClient client,
        [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D gsWhiteLogoTexture,
        [Inject] StandardLevelDetailViewController standardLevelDetailViewController
        ) : base("MapRankedStats")
    {
        _guildSaberCache = guildSaberCache;
        _config = config;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _gsWhiteLogoTexture = gsWhiteLogoTexture;

        var standardLevelDetailView = standardLevelDetailViewController._standardLevelDetailView;

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

            // Move the ranked stats UI to the right of the levelParamsPanel.
            RTransform.offsetMin = new Vector2(80, -5);

            standardLevelDetailViewController.didChangeContentEvent += BeatmapContentChanged;
            standardLevelDetailView.didChangeDifficultyBeatmapEvent += BeatmapDifficultyChanged;
        });

        // Attach the ranked stats UI to the level params panel.
        BuildUI(standardLevelDetailView._levelParamsPanel.transform);
    }

    public sealed override void BuildUI(Transform parent) => base.BuildUI(parent);

    protected async Task UpdateRankedStats(BeatmapLevel? beatmapLevel, BeatmapKey? beatmapKeyHolder)
    {
        if (!_config.RankedMapStats.Enabled) return;

        if (beatmapLevel == null
            || beatmapKeyHolder is not { } beatmapKey
            || !SongHash.TryCreate(Hashing.ComputeCustomLevelHash(beatmapLevel)).TryGetValue(out var songHash))
        {
            SetActive(false);
            return;
        }

        var rankedMap = await FetchRankedMap(
            _config.PlayerCard.ContextId,
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

        var guildIconTexture = await _guildSaberCache.FetchGuildIconTexture(_config.PlayerCard.GuildId, _client)
                               ?? _gsWhiteLogoTexture;
        var roundedIcon = await TextureUtils.CreateRoundedTextureAsync(
            guildIconTexture, guildIconTexture.width * 0.2f);

        if (rankedMap.CategoryIds.Length == 0)
        {
            _categoryIcon.SetActive(false);
            _mapCategories.SetActive(false);
        }
        else
        {
            var categories = _guildSaberCache.GuildsExtended[_config.PlayerCard.GuildId].Categories
                .Where(x => rankedMap.CategoryIds.Contains(x.Id)).ToArray();

            var hasCategoryIcon = false;
            if (categories.Length == 1)
            {
                var categoryIconTexture = await _guildSaberCache.FetchCategoryIconTexture(categories[0].Id, _client);
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
        _guildIcon.SetSprite(Sprite.Create(
            roundedIcon,
            new Rect(0, 0, roundedIcon.width, roundedIcon.height),
            Vector2.zero));
        _guildIcon.SetActive(true);

        SetActive(true);
    }

    public void BeatmapContentChanged(
        StandardLevelDetailViewController standardLevelDetailView,
        StandardLevelDetailViewController.ContentType contentType)
    {
        if (standardLevelDetailView == null) return;

        var beatmapKey = standardLevelDetailView.beatmapKey;
        var beatmapLevel = standardLevelDetailView.beatmapLevel;
        _beatmapLevel = beatmapLevel;

        _ = UpdateRankedStats(beatmapLevel, beatmapKey);
    }

    public void BeatmapDifficultyChanged(StandardLevelDetailView standardLevelDetailView)
    {
        if (standardLevelDetailView == null) return;
        _ = UpdateRankedStats(_beatmapLevel, standardLevelDetailView.beatmapKey);
    }

    public async Task<RankedMapResponses.RankedMap?> FetchRankedMap(
        ContextId contextId, SongHash hash, string mode, EDifficulty difficulty, GuildSaberClient client)
        => (await _guildSaberCache.FetchRankedMaps(contextId, hash, client))
            .FirstOrDefault(x => x.Versions
                .Any(v => v.Difficulty.GameMode == mode && v.Difficulty.Difficulty == difficulty));
}