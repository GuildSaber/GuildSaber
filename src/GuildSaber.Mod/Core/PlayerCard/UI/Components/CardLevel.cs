using GuildSaber.Mod.Core.UI.Common;

namespace GuildSaber.Mod.Core.PlayerCard.UI.Components;

public class CardLevel : GSText
{
    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    protected CardLevel() : base("PlayerCardLevel", string.Empty) { }

    public static CardLevel Make() => new();

    //////////////////////////////////////////////////////
    /////////////////////////////////////////////////////

    public void SetLevel(string name, float level)
    {
        SetText($"{name}\n{level:0}");
        SetFontSize(3.7f);
    }
}