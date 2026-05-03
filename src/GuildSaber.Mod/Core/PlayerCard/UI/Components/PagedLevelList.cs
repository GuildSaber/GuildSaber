using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PagedLevelList : XUIVLayout
{
    private static readonly int LevelsCountByPage = 4;
    private readonly PluginConfig _config = null!;
    private readonly GuildSaberCache _guildSaberCache;
    private readonly List<GSText> _levels = [];

    private readonly UIFactory _uiFactory;

    private int _maxPage;
    private int _page;

    private GSSecondaryButton _pageLeftButton = null!;
    private GSSecondaryButton _pageRightButton = null!;
    private XUIGLayout _playerLevelsContainer = null!;
    private int _totalLevelCount;

    public PagedLevelList(UIFactory factory, GuildSaberCache guildSaberCache, PluginConfig config) : base(
        "PagedLevelList")
    {
        _uiFactory = factory;
        _guildSaberCache = guildSaberCache;
        _config = config;

        OnReady(x =>
        {
            XUIGLayout.Make().Bind(ref _playerLevelsContainer).SetSpacing(new Vector2(0, -1)).SetMinWidth(30)
                .SetCellSize(new Vector2(18, 12f)).SetConstraintCount(2).BuildUI(x.transform);

            XUIHLayout.Make(
                    factory.SecondaryButton("<", PageLeft)
                        .SetWidth(5)
                        .SetHeight(5)
                        .Bind(ref _pageLeftButton),
                    factory.SecondaryButton(">", PageRight)
                        .SetWidth(5)
                        .SetHeight(5)
                        .Bind(ref _pageRightButton)
                ).SetSpacing(10)
                .SetPadding(new RectOffset(-5, 2, 2, 2))
                .BuildUI(x.transform);
        });
    }

    public static PagedLevelList Make(UIFactory factory, GuildSaberCache guildSaberCache, PluginConfig config)
        => new(factory, guildSaberCache, config);

    public PagedLevelList Bind(ref PagedLevelList target)
    {
        target = this;
        return this;
    }

    public void Refresh(GuildSaberCache targetData)
    {
        // Same instance, I don't think we need to set again the instance.
        //_guildSaberCache = targetData;

        _page = 0;

        Refresh();
    }

    public void Refresh()
    {
        if (_guildSaberCache.MemberLevelStats.Count == 0)
            //_logger.Warn("No levels ???");
            //DisplayLevelsDetails(false);
            return;

        if (_levels.Count == 0)
            for (var i = 0; i < LevelsCountByPage; i++)
            {
                var level = _uiFactory.Text("");
                level.BuildUI(_playerLevelsContainer.Element.transform);
                _levels.Add(level);
            }

        HideAllLevels();

        var page = _page;

        LevelStatResponses.MemberLevelStat[] allLevels = [];
        var allCategories = _guildSaberCache.GuildsExtended[_config.PlayerCard.GuildId].Categories;

        foreach (var category in allCategories)
        {
            var levelArray = _guildSaberCache.MemberLevelStats[_config.PlayerCard.ContextId].Where(x =>
                x.Level.CategoryId == category.Id && x.Level.CategoryId != null && x.IsCompleted);

            LevelStatResponses.MemberLevelStat? toAppend = null;

            if (levelArray.Any())
            {
                toAppend = levelArray.Last();
            }
            else
            {
                levelArray = allLevels.Where(x => x.Level.CategoryId == category.Id);

                if (levelArray.Any()) toAppend = levelArray.First();
            }

            if (toAppend != null)
                allLevels = allLevels.Append(toAppend).ToArray();
            else
                allCategories = allCategories.Where(x => x.Id != category.Id).ToArray();
        }

        _totalLevelCount = allLevels.Length;
        _maxPage = _totalLevelCount / LevelsCountByPage;

        var displayedLevels = new List<LevelStatResponses.MemberLevelStat>();
        var displayedCategories = new List<CategoryResponses.Category>();
        for (var i = 0; i < allLevels.Length; i++)
            if (i >= LevelsCountByPage * page && i < LevelsCountByPage * (page + 1))
            {
                displayedLevels.Add(allLevels[i]);
                displayedCategories.Add(allCategories[i]);
            }

        for (var i = 0; i < displayedLevels.Count; i++)
        {
            var category = displayedLevels[i];
            if (category == null)
            {
                //Logger.Instance.Error($"Could not get category with id {l_DisplayedCategories[l_i].CategoryID}");
                _levels[i].SetActive(false);
                continue;
            }

            _levels[i].SetText($"{displayedCategories[i].Info.Name}\n{displayedLevels[i].Level.Order}");
        }

        _pageLeftButton.SetActive(_totalLevelCount > LevelsCountByPage && _page != 0);
        _pageRightButton.SetActive(_totalLevelCount > LevelsCountByPage * (_page + 1));
    }

    public void PageLeft()
    {
        if (_page > 0) _page--;

        Refresh();
    }

    public void PageRight()
    {
        if (_page < _maxPage) _page++;

        Refresh();
    }

    public void HideAllLevels()
    {
        foreach (var x in _levels) x.SetText(string.Empty);
    }
}