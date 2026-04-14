using System.Collections.Generic;
using CP_SDK.XUI;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PagedLevelList : XUIVLayout
{
    protected PagedLevelList(string name, params IXUIElement[] childs) : base(name, childs)
    {
        OnReady(x =>
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
    }

    public static PagedLevelList Make()
    {
        return new PagedLevelList("PagedLevelList");
    }

    protected XUIGLayout PlayerLevelsContainer = null!;
    protected GSSecondaryButton PageLeftButton = null!;
    protected GSSecondaryButton PageRightButton = null!;
    protected ModData _modData = null!;
    protected int _page = 0;
    protected List<CardLevel> Levels = new List<CardLevel>();
    
    public PagedLevelList Bind(ref PagedLevelList target)
    {
        target = this;
        return this;
    }
    
    public void Refresh(ModData targetData)
    {
        _modData = targetData;

        Refresh();
    }
    
    public void Refresh()
    {
        _modData.GetGuildLogo()
    }

    public void PageLeft()
    {
        
    }

    public void PageRight()
    {
        
    }
}