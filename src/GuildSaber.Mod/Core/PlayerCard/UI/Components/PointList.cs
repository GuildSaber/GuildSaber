using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PointList : XUIVLayout
{
    protected readonly List<CardPoints> _pointTexts = new();

    protected ModData _modData = null!;
    protected PointList(string name, params IXUIElement[] childs) : base(name, childs) { }

    public static PointList Make() => new("PointList");

    public PointList Bind(ref PointList x)
    {
        x = this;
        return this;
    }

    public void Refresh(ModData modData) => _modData = modData;

    public void Refresh()
    {
        var l_Points = _modData.PlayerPoints;

        //Logger.Instance.Info(l_Points.Count.ToString());
        foreach (var l_Item in _pointTexts)
            l_Item.SetActive(false);

        for (var l_i = 0; l_i < l_Points.Count(); l_i++)
        {
            if (_pointTexts.Count - 1 < l_i)
            {
                var l_Point = CardPoints.Make();
                l_Point.BuildUI(Element.transform);
                _pointTexts.Add(l_Point);
            }

            _pointTexts[l_i].SetPoints(l_Points.ElementAt(l_i), _modData.CardUsedColor);
            _pointTexts[l_i].SetActive(true);
        }
    }
}