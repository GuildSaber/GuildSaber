using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PointList : XUIVLayout
{
    protected readonly List<GSText> _pointTexts = new();

    protected ModData _modData;
    protected UIFactory _uiFactory;

    protected PointList(ModData modData, UIFactory factory) : base("PointList", [])
    {
        _uiFactory = factory;
        _modData = modData;
    }

    public static PointList Make(ModData modData, UIFactory factory) => new(modData, factory);

    public PointList Bind(ref PointList x)
    {
        x = this;
        return this;
    }

    public void Refresh(ModData modData)
    {
        _modData = modData;
        Refresh();
    }

    public void Refresh()
    {
        var points = _modData.PlayerPoints;

        //Logger.Instance.Info(points.Count.ToString());
        foreach (var item in _pointTexts)
            item.SetActive(false);

        for (var i = 0; i < points.Count(); i++)
        {
            if (_pointTexts.Count - 1 < i)
            {
                var pointText = _uiFactory.Text("");
                pointText.BuildUI(Element.transform);
                _pointTexts.Add(pointText);
            }

            var point = points.ElementAt(i);
            var htmlColor = ColorUtility.ToHtmlStringRGB(_modData.CardUsedColor);
            _pointTexts[i].SetText($"<color=#{htmlColor}>{point.Name}</color>#<color=#{htmlColor}>{point.Rank:0}</color>");

            _pointTexts[i].SetActive(true);
        }
    }
}