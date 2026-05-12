using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class PointList : XUIVLayout
{
    private readonly GuildSaberConfig _config;
    private readonly GuildSaberCache _guildSaberCache;
    private readonly List<GSText> _pointTexts = [];
    private readonly UIFactory _uiFactory;

    protected PointList(GuildSaberCache guildSaberCache, GuildSaberConfig config, UIFactory factory) : base("PointList")
    {
        _uiFactory = factory;
        _config = config;
        _guildSaberCache = guildSaberCache;
    }

    public static PointList Make(GuildSaberCache guildSaberCache, GuildSaberConfig config, UIFactory factory)
        => new(guildSaberCache, config, factory);

    public void Refresh()
    {
        var points = _guildSaberCache.MemberContextStats[_config.ContextId].SimplePointsWithRank
            .Where(x => x.CategoryId is null)
            .ToArray();

        foreach (var item in _pointTexts)
            item.SetActive(false);

        var useCustomColors =
            _guildSaberCache.PlayerExtended?.Player is not null
            && _config.PlayerCard.ColorSettings.UseCustomColors
            && PlayerCardLibrary.CanPlayerUseCustomColors(
                _guildSaberCache.MemberLevelStats[_config.ContextId],
                _guildSaberCache.PlayerExtended.Player);

        for (var i = 0; i < points.Length; i++)
        {
            var stat = points[i];

            // Ensure we have enough text elements. If not, create them.
            if (_pointTexts.Count - 1 < i)
            {
                var pointText = _uiFactory.Text("");
                pointText.BuildUI(Element.transform);
                _pointTexts.Add(pointText);
            }

            // Check if there is too much text elements to remove them.
            if (_pointTexts.Count - 1 > points.Length)
                for (var j = _pointTexts.Count - 1; j >= points.Length; j--)
                {
                    _pointTexts[j].SetActive(false);
                    _pointTexts.RemoveAt(j);
                }

            _pointTexts[i]
                .SetActive(true)
                .SetText($"{stat.Points:0.##} {stat.Name} (#{stat.Rank})");

            if (useCustomColors)
                _pointTexts[i].SetColor(_config.PlayerCard.ColorSettings.MainCardColor);
        }
    }

    public PointList Bind(ref PointList x) => x = this;
}