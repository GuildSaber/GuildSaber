using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Extensions;

public static class ColorExtensions
{
    public static Color ColorWithAlpha(this Color x, float alpha)
    {
        x.a = alpha;
        return x;
    }
}