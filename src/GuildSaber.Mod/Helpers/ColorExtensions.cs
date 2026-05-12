using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using UnityEngine;

namespace GuildSaber.Mod.Helpers;

public static class ColorExtensions
{
    extension(Color self)
    {
        public Color WithAlpha(float alpha)
        {
            self.a = alpha;
            return self;
        }

        public static Color FromArgb(int argb)
        {
            var r = (byte)(argb >> 16 & 0xFF);
            var g = (byte)(argb >> 8 & 0xFF);
            var b = (byte)(argb & 0xFF);
            return new Color(r, g, b);
        }

        public static Color FromDifficulty(EDifficulty difficulty) => difficulty switch
        {
            EDifficulty.Easy => Color.FromArgb(0x3CB371),
            EDifficulty.Normal => Color.FromArgb(0x59B0F4),
            EDifficulty.Hard => Color.FromArgb(0xEE5E44),
            EDifficulty.Expert => Color.FromArgb(0xBF2A42),
            EDifficulty.ExpertPlus => Color.FromArgb(0x8F48DB),
            _ => Color.FromArgb(0xFFFFFF)
        };
    }
}