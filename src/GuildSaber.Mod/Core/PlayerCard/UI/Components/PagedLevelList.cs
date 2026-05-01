using System.Collections.Generic;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Levels;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PagedLevelList : XUIVLayout
{
    private static readonly int LevelsCountByPage = 5;

    private int _maxPage;
    private ModData _modData = null!;
    private int _page;
    private readonly List<GSText> _levels = [];

    private GSSecondaryButton _pageLeftButton = null!;
    private GSSecondaryButton _pageRightButton = null!;
    private XUIGLayout _playerLevelsContainer = null!;

    private readonly UIFactory _uiFactory;

    public PagedLevelList(UIFactory factory) : base("PagedLevelList", [])
    {
        _uiFactory = factory;

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

    public static PagedLevelList Make(UIFactory factory) => new(factory);

    public PagedLevelList Bind(ref PagedLevelList target)
    {
        target = this;
        return this;
    }

    public void Refresh(ModData targetData)
    {
        _modData = targetData;

        _maxPage = _modData.PlayerLevels.Length / LevelsCountByPage;
        _page = 0;

        Refresh();
    }

    public void Refresh()
    {
        if (_modData.PlayerLevels.Length == 0)
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

        var allCategories = _modData.PlayerLevels;
        var displayedCategories = new List<LevelStatResponses.MemberLevelStat>();
        for (var l_i = 0; l_i < allCategories.Length; l_i++)
            if (l_i >= LevelsCountByPage * page && l_i < LevelsCountByPage * (page + 1))
                displayedCategories.Add(allCategories[l_i]);

        for (var i = 0; i < displayedCategories.Count; i++)
        {
            
            
            var category = displayedCategories[i];
            if (category == null)
            {
                //Logger.Instance.Error($"Could not get category with id {l_DisplayedCategories[l_i].CategoryID}");
                _levels[i].SetActive(false);
                continue;
            }

            if (category.Level is not LevelResponses.Level.RankedMapListLevel)
            {
                _levels[i].SetActive(false);
                continue;
            }

            _levels[i].SetText($"{displayedCategories[i].Level.Info.Name}\n{displayedCategories[i].Level.Order}");
        }

        _pageLeftButton.SetActive(_modData.PlayerLevels.Length > LevelsCountByPage && _page != 0);
        _pageRightButton.SetActive(_modData.PlayerLevels.Length > LevelsCountByPage * (_page + 1));
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