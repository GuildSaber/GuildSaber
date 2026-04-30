using System.Collections.Generic;
using CP_SDK.XUI;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PagedLevelList : XUIVLayout
{
    protected static readonly int LevelsCountByPage = 5;

    protected int _maxPage;
    protected ModData _modData = null!;
    protected int _page;
    protected List<CardLevel> Levels = [];
    protected GSSecondaryButton PageLeftButton = null!;
    protected GSSecondaryButton PageRightButton = null!;

    protected XUIGLayout PlayerLevelsContainer = null!;

    protected PagedLevelList(string name, params IXUIElement[] childs) : base(name, childs) => OnReady(x =>
    {
        XUIGLayout.Make().Bind(ref PlayerLevelsContainer).SetSpacing(new Vector2(0, -1)).SetMinWidth(30)
            .SetCellSize(new Vector2(18, 12f)).SetConstraintCount(2).BuildUI(x.transform);

        XUIHLayout.Make(
                GSSecondaryButton.Make("<", PageLeft)
                    .SetWidth(5)
                    .SetHeight(5)
                    .Bind(ref PageLeftButton),
                GSSecondaryButton.Make(">", PageRight)
                    .SetWidth(5)
                    .SetHeight(5)
                    .Bind(ref PageRightButton)
            ).SetSpacing(10)
            .SetPadding(new RectOffset(-5, 2, 2, 2))
            .BuildUI(x.transform);
    });

    public static PagedLevelList Make() => new("PagedLevelList");

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

        if (Levels.Count == 0)
            for (var l_i = 0; l_i < LevelsCountByPage; l_i++)
            {
                var l_Level = CardLevel.Make();
                l_Level.BuildUI(PlayerLevelsContainer.Element.transform);
                Levels.Add(l_Level);
            }

        HideAllLevels();

        var l_Page = _page;

        var l_AllCategories = _modData.PlayerLevels;
        var l_DisplayedCategories = new List<LevelStatResponses.MemberLevelStat>();
        for (var l_i = 0; l_i < l_AllCategories.Length; l_i++)
            if (l_i >= LevelsCountByPage * l_Page && l_i < LevelsCountByPage * (l_Page + 1))
                l_DisplayedCategories.Add(l_AllCategories[l_i]);

        for (var l_i = 0; l_i < l_DisplayedCategories.Count; l_i++)
        {
            //CardLevels[l_i].SetActive(true);
            var l_Category = l_DisplayedCategories[l_i];
            if (l_Category == null)
            {
                //Logger.Instance.Error($"Could not get category with id {l_DisplayedCategories[l_i].CategoryID}");
                Levels[l_i].SetActive(false);
                continue;
            }

            Levels[l_i].SetLevel(l_DisplayedCategories[l_i].Level.Info.Name, l_DisplayedCategories[l_i].Level.Order);
        }

        PageLeftButton.SetActive(_modData.PlayerLevels.Length > LevelsCountByPage && _page != 0);
        PageRightButton.SetActive(_modData.PlayerLevels.Length > LevelsCountByPage * (_page + 1));
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
        foreach (var x in Levels) x.SetText(string.Empty);
    }
}