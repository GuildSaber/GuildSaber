using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Mod.Configurations;
using GuildSaber.Mod.Core.UI;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class PointList : XUIVLayout
{
    protected readonly List<GSText> _pointTexts = new();
    protected PluginConfig _config = null!;
    protected UIFactory _uiFactory;

    protected GuildSaberCache GuildSaberCache;

    protected PointList(GuildSaberCache guildSaberCache, PluginConfig config, UIFactory factory) : base("PointList")
    {
        _uiFactory = factory;
        _config = config;
        GuildSaberCache = guildSaberCache;
    }

    public static PointList Make(GuildSaberCache guildSaberCache, PluginConfig config, UIFactory factory)
        => new(guildSaberCache, config, factory);

    public PointList Bind(ref PointList x)
    {
        x = this;
        return this;
    }

    public void Refresh(GuildSaberCache guildSaberCache)
    {
        GuildSaberCache = guildSaberCache;
        Refresh();
    }

    public void Refresh()
    {
        var points = GuildSaberCache.MemberContextStats[_config.PlayerCard.ContextId].SimplePointsWithRank
            .Where(x => x.CategoryId is null).ToArray();

        foreach (var item in _pointTexts)
            item.SetActive(false);

        for (var i = 0; i < points.Length; i++)
        {
            if (_pointTexts.Count - 1 < i)
            {
                var pointText = _uiFactory.Text("");
                pointText.BuildUI(Element.transform);
                _pointTexts.Add(pointText);
            }

            var point = points.ElementAt(i);
            var htmlColor = ColorUtility.ToHtmlStringRGB(_config.PlayerCard.ColorSettings.MainCardColor);
            _pointTexts[i]
                .SetText($"<color=#{htmlColor}>{point.Name}</color>#<color=#{htmlColor}>{point.Rank:0}</color>");

            _pointTexts[i].SetActive(true);
        }
    }
}