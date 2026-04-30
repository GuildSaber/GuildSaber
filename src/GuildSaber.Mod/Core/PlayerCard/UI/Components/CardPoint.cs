using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class CardPoints : GSText
{
    protected CardPoints(string name, string text) : base(name, text) { }

    public static CardPoints Make() => new("CardPoints", string.Empty);

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    public void SetPoints(ContextStatResponses.SimplePointWithRank x, Color color)
    {
        var htmlColor = ColorUtility.ToHtmlStringRGB(color);
        SetText($"<color=#{htmlColor}>{x.Name}</color>#<color=#{htmlColor}>{x.Rank:0}</color>");
    }
}