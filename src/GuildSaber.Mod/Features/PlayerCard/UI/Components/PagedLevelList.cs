using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.CSharpClient.Routes.Guilds.Members.LevelStats;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Helpers;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class PagedLevelList : XUIVLayout
{
    private const int LevelsCountByPage = 4;

    private readonly GuildSaberConfig _config;
    private readonly GuildSaberCache _guildSaberCache;
    private readonly List<GSText> _levelTextInCurrentPage = [];

    private int _currentPage;
    private Logger _logger;

    private GSSecondaryButton _pageLeftButton = null!;
    private GSSecondaryButton _pageRightButton = null!;
    private XUIGLayout _playerLevelsContainer = null!;

    public PagedLevelList(UIFactory factory, GuildSaberCache guildSaberCache, GuildSaberConfig config, Logger logger)
        : base("PagedLevelList")
    {
        var uiFactory = factory;
        _guildSaberCache = guildSaberCache;
        _config = config;
        _logger = logger;

        OnReady(x =>
        {
            XUIGLayout.Make()
                .Bind(ref _playerLevelsContainer)
                .SetSpacing(new Vector2(0, -1))
                .SetMinWidth(30)
                .SetCellSize(new Vector2(18, 12f))
                .SetConstraintCount(2)
                .BuildUI(x.transform);

            // Since the max number of levels is knows ahead, allocate them all and reuse them in pagination.
            for (var i = 0; i < LevelsCountByPage; i++)
            {
                var level = uiFactory.Text("");
                level.BuildUI(_playerLevelsContainer.Element.transform);
                _levelTextInCurrentPage.Add(level);
            }

            XUIHLayout.Make(
                    factory.SecondaryButton("<", PageLeft)
                        .Bind(ref _pageLeftButton)
                        .SetColor(Color.white)
                        .SetWidth(5)
                        .SetHeight(5),
                    factory.SecondaryButton(">", PageRight)
                        .Bind(ref _pageRightButton)
                        .SetColor(Color.white)
                        .SetWidth(5)
                        .SetHeight(5)
                ).SetSpacing(10)
                .SetPadding(new RectOffset(-5, 2, 2, 2))
                .BuildUI(x.transform);
        });
    }

    public readonly record struct CategoryLevelData(string CategoryName, string LevelName, Color Color);

    public static PagedLevelList Make(
        UIFactory factory, GuildSaberCache guildSaberCache, GuildSaberConfig config, Logger logger)
        => new(factory, guildSaberCache, config, logger);

    public void CleanRefresh()
    {
        _currentPage = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        EmptyCurrentLevelTexts();
        if (_guildSaberCache.MemberLevelStats.Count == 0)
            return;

        var categories = _guildSaberCache.GuildsExtended[_config.GuildId].Categories;
        var levelStats = _guildSaberCache.MemberLevelStats[_config.ContextId];

        var categoryLevels = categories
            .Select(category => (category, level: levelStats.GetCategoryLevel(category.Id)))
            .Select(tuple => new CategoryLevelData(
                tuple.category.Info.Name,
                tuple.level?.Info.Name ?? "None",
                Color.FromArgb(tuple.level?.Info.Color ?? 0xFFFFFF)))
            .ToArray();

        var maxPage = (categoryLevels.Length - 1) / LevelsCountByPage;
        if (_currentPage < 0 || _currentPage > maxPage)
            _currentPage = 0;

        var displayedCategoryLevel = categoryLevels
            .Skip(_currentPage * LevelsCountByPage)
            .Take(LevelsCountByPage)
            .ToArray();

        for (var i = 0; i < displayedCategoryLevel.Length; i++)
        {
            var categoryLevel = displayedCategoryLevel[i];
            _levelTextInCurrentPage[i].SetText($"{categoryLevel.CategoryName}\n{categoryLevel.LevelName}");
        }

        var (leftPageAvailable, rightPageAvailable) = (
            categoryLevels.Length > LevelsCountByPage && _currentPage != 0,
            categoryLevels.Length > LevelsCountByPage * (_currentPage + 1)
        );

        _pageRightButton
            .SetColor(rightPageAvailable ? Color.white : Color.white.WithAlpha(0.3f))
            .SetInteractable(rightPageAvailable);

        _pageLeftButton
            .SetColor(leftPageAvailable ? Color.white : Color.white.WithAlpha(0.3f))
            .SetInteractable(leftPageAvailable);
    }

    public void PageLeft()
    {
        _currentPage--;
        RefreshUI();
    }

    public void PageRight()
    {
        _currentPage++;
        RefreshUI();
    }

    public void EmptyCurrentLevelTexts()
    {
        foreach (var x in _levelTextInCurrentPage) x.SetText(string.Empty);
    }

    public PagedLevelList Bind(ref PagedLevelList x) => x = this;
}