using GuildSaber.Api.Features.Guilds.Members.ContextStats;
using GuildSaber.Mod.Core.UI.Common;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class CardPoints : GSText
{
    public static CardPoints Make()
    {
        return new CardPoints("CardPoints", string.Empty);
    }

    protected CardPoints(string p_Name, string p_Text) : base(p_Name, p_Text)
    {
    }

    ///////////////////////////////////////////////////////
    //////////////////////////////////////////////////////

    public void SetPoints(ContextStatResponses.SimplePointWithRank x, Color color)
    {

        string l_Color = ColorUtility.ToHtmlStringRGB(color);
        SetText($"<color=#{l_Color}>{x.Name}</color>#<color=#{l_Color}>{x.Rank:0}</color>");
    }
}