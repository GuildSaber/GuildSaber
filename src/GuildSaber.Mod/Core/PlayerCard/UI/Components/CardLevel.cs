using GuildSaber.Mod.Core.UI.Common;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class CardLevel : GSText
{
    public static CardLevel Make()
    {
        return new CardLevel();
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    protected CardLevel() : base("PlayerCardLevel", string.Empty)
    {
    }

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////
        
    public void SetLevel(string name, float level)
    {
        SetText($"{name}\n{level:0}");
        SetFontSize(3.7f);
    }
}