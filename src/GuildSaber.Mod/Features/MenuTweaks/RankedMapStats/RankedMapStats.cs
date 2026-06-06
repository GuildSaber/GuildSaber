using System;
using System.Linq;
using System.Threading.Tasks;
using CP_SDK.XUI;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.RankedScores.Http;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.RankedMap;
using GuildSaber.Mod.Helpers;
using GuildSaber.Mod.Resources;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GuildSaber.Mod.Features.MenuTweaks.RankedMapStats;

/// <summary>
/// Displays a map's ranked stats if ranked (map categories, map level, and if the player has passed the map).
/// </summary>
public class RankedMapStats(
    [Inject(Id = nameof(ResourceMap.GsWhiteLogo))] Texture2D placeHolderIcon,
    [Inject(Id = nameof(ResourceMap.CheckMark))] Texture2D checkMarkTexture,
    [Inject(Id = nameof(ResourceMap.DenyMark))] Texture2D denyMarkTexture,
    [Inject(Id = nameof(ResourceMap.QuestionMark))] Texture2D questionMarkTexture,
    [Inject(Id = nameof(ResourceMap.CheckShield))] Texture2D checkShieldTexture,
    [Inject(Id = nameof(ResourceMap.DenyShield))] Texture2D denyShieldTexture,
    GuildSaberClient client,
    GuildSaberConfig config,
    GuildSaberCache cache,
    RankedMapManager rankedMapManager,
    StandardLevelDetailView standardLevelDetailView,
    UIFactory factory,
    Logger logger
) : XUIVLayout("MapRankedStats"), IInitializable, IDisposable
{
    private XUIImage _categoryIcon = null!;
    private XUIImage _guildIcon = null!;
    private GSText _mapCategories = null!;
    private GSText _mapLevel = null!;
    private XUIImage _whiteMarkImage = null!;

    public void Dispose()
    {
        logger.Info("Disposing..");
        rankedMapManager.OnMapSelected -= OnMapSelected;
    }

    public void Initialize()
    {
        logger.Info("Preparing UI");
        rankedMapManager.OnMapSelected += OnMapSelected;

        OnReady(element =>
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
                .BuildUI(element.transform);

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
            ).BuildUI(element.transform);

            XUIImage.Make(Sprite.Create(checkMarkTexture,
                    new Rect(0, 0, checkMarkTexture.width, checkMarkTexture.height), Vector2.zero)
                ).Bind(ref _whiteMarkImage)
                .SetWidth(3)
                .SetHeight(3)
                .SetActive(false)
                .OnReady(i => i.LElement.ignoreLayout = true)
                .BuildUI(element.transform);

            element.SetSpacing(2);
            element.VLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            element.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            element.CSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            // Move the ranked stats UI to the right of the levelParamsPanel.
            RTransform.offsetMin = new Vector2(80, -5);
        });

        BuildUI(standardLevelDetailView._levelParamsPanel.transform);
        logger.Info("UI created and attached to levelParamsPanel");
    }

    private void OnMapSelected(RankedMapEventData eventData)
    {
        if (!config.RankedMapStats.Enabled || eventData.RankedMapWithScores is null)
        {
            SetActive(false);
            return;
        }

        _ = UpdateUI(eventData.RankedMapWithScores);
    }

    private async Task UpdateUI(RankedMapResponses.RankedMapWithScores rankedMapWithScoresOfPlayer)
    {
        var (rankedMap, rankedScores) = rankedMapWithScoresOfPlayer;
        var guildIconTexture = await cache.FetchGuildIconTexture(config.GuildId, client) ?? placeHolderIcon;
        var roundedIcon = await TextureUtils.CreateRoundedTextureAsync(guildIconTexture, guildIconTexture.width * 0.2f);

        if (rankedMap.CategoryIds.Length == 0)
        {
            _categoryIcon.SetActive(false);
            _mapCategories.SetActive(false);
        }
        else
        {
            var categories = cache.GuildsExtended[config.GuildId].Categories
                .Where(x => rankedMap.CategoryIds.Contains(x.Id)).ToArray();

            var hasCategoryIcon = false;
            if (categories.Length == 1)
            {
                var categoryIconTexture = await cache.FetchCategoryIconTexture(categories[0].Id, client);
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

        var playerPassedTheMap = rankedScores
            .Any(x => x.State.HasFlag(RankedScoreResponses.EState.Selected)
                      && (x.State & RankedScoreResponses.EState.NonPointGiving) == 0);

        var isRefused = rankedScores.Any(x => x.State.HasFlag(RankedScoreResponses.EState.Refused));
        var isPending = rankedScores.Any(x => x.State.HasFlag(RankedScoreResponses.EState.Pending));
        var isConfirmed = rankedScores.Any(x => x.State.HasFlag(RankedScoreResponses.EState.Confirmed));
        var isDenied = rankedScores.Any(x => x.State.HasFlag(RankedScoreResponses.EState.Denied));

        _whiteMarkImage.SetActive(playerPassedTheMap || isRefused || isPending || isDenied);

        var usedTexture = (playerPassedTheMap, isRefused, isDenied, isPending, isConfirmed) switch
        {
            (playerPassedTheMap: true, false, false, false, false) => checkMarkTexture,
            (false, false, isDenied: true, false, false) => denyMarkTexture,
            (false, false, false, isPending: true, false) => questionMarkTexture,
            (playerPassedTheMap: true, false, false, false, isConfirmed: true) => checkShieldTexture,
            (false, isRefused: true, false, false, false) => denyShieldTexture,
            _ => throw new InvalidOperationException("Incompatible states")
        };

        var resultSprite =
            Sprite.Create(usedTexture, new Rect(0, 0, usedTexture.width, usedTexture.height), Vector2.zero);
        _whiteMarkImage.SetSprite(resultSprite);

        SetActive(true);
    }

    public sealed override void BuildUI(Transform parent) => base.BuildUI(parent);
}