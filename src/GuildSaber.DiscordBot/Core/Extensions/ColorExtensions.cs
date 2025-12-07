using Discord;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class ColorExtensions
{
    extension(Color)
    {
        public static Color FromArgb(int argb)
        {
            var r = (byte)(argb >> 16 & 0xFF);
            var g = (byte)(argb >> 8 & 0xFF);
            var b = (byte)(argb & 0xFF);
            return new Color(r, g, b);
        }
    }
}