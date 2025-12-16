using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using Color = Discord.Color;

namespace GuildSaber.DiscordBot.Core.Extensions;

public static class DiscordColorExtensions
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

        public static Color FromDifficulty(EDifficulty difficulty) => difficulty switch
        {
            EDifficulty.Easy => FromArgb(0x3CB371),
            EDifficulty.Normal => FromArgb(0x59B0F4),
            EDifficulty.Hard => FromArgb(0xEE5E44),
            EDifficulty.Expert => FromArgb(0xBF2A42),
            EDifficulty.ExpertPlus => FromArgb(0x8F48DB),
            _ => FromArgb(0xFFFFFF)
        };
    }
}

public static class ImageSharpColorExtensions
{
    extension(SixLabors.ImageSharp.Color)
    {
        public static SixLabors.ImageSharp.Color FromArgb(int argb)
        {
            var r = (byte)(argb >> 16 & 0xFF);
            var g = (byte)(argb >> 8 & 0xFF);
            var b = (byte)(argb & 0xFF);
            return SixLabors.ImageSharp.Color.FromRgb(r, g, b);
        }
    }

    public static IPath ToRoundedRectangle(this RectangleF rectangle, float cornerRadius) => new PathBuilder()
        .AddArc(new PointF(rectangle.Left + cornerRadius, rectangle.Top + cornerRadius), cornerRadius, cornerRadius, 0,
            180, 90)
        .AddArc(new PointF(rectangle.Right - cornerRadius, rectangle.Top + cornerRadius), cornerRadius, cornerRadius, 0,
            270, 90)
        .AddArc(new PointF(rectangle.Right - cornerRadius, rectangle.Bottom - cornerRadius), cornerRadius, cornerRadius,
            0, 0, 90)
        .AddArc(new PointF(rectangle.Left + cornerRadius, rectangle.Bottom - cornerRadius), cornerRadius, cornerRadius,
            0, 90, 90)
        .CloseFigure()
        .Build();
}