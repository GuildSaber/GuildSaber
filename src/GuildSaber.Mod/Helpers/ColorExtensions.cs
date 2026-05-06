using UnityEngine;

namespace GuildSaber.Mod.Helpers;

public static class ColorExtensions
{
    public static Color ColorWithAlpha(this Color x, float alpha)
    {
        x.a = alpha;
        return x;
    }
}