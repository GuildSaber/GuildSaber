using System.Collections.Generic;
using System.Linq;
using CP_SDK.XUI;
using GuildSaber.Mod.Features.Common.UI;
using GuildSaber.Mod.Features.Common.UI.Components;
using GuildSaber.Mod.Features.GuildSaber;
using GuildSaber.Mod.Features.GuildSaber.Runtime;

namespace GuildSaber.Mod.Features.PlayerCard.UI.Components;

public class PointList(GuildSaberSession session, GuildSaberConfig config, UIFactory uiFactory)
    : XUIVLayout("PointList")
{
    private readonly List<GSText> _pointTexts = [];

    public static PointList Make(GuildSaberSession session, GuildSaberConfig config, UIFactory factory)
        => new(session, config, factory);

    public void Refresh()
    {
        var points = session.CurrentMemberContextStats.SimplePointsWithRank
            .Where(x => x.CategoryId is null)
            .ToArray();

        foreach (var item in _pointTexts)
            item.SetActive(false);

        var useCustomColors = config.PlayerCard.ColorSettings.UseCustomColors && PlayerCardLibrary
            .CanPlayerUseCustomColors(session.CurrentMemberLevelStats, session.PlayerExtended.Player);

        for (var i = 0; i < points.Length; i++)
        {
            var stat = points[i];

            // Ensure we have enough text elements. If not, create them.
            if (_pointTexts.Count - 1 < i)
            {
                var pointText = uiFactory.Text("");
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
                _pointTexts[i].SetColor(config.PlayerCard.ColorSettings.MainCardColor);
        }
    }

    public PointList Bind(ref PointList x) => x = this;
}