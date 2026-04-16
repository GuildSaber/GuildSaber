using System;
using System.Threading.Tasks;
using GuildSaber.Mod.Core.PlayerCard;
using GuildSaber.Mod.Core.UI.Extensions;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Utils;

// TODO: Turn this garbage into shader

internal static class TextureUtils
{
    public enum EGradientDirection
    {
        Horizontal,
        Vertical
    }

    private static readonly Color m_TransparentColor = new(0, 0, 0, 0);
    
    public static async Task<Texture2D?> GetImage(string p_Url, PlayerCardResources resources)
    {
        var l_NewTexture = new Texture2D(100, 100);

        try {
            using (var l_Client = new System.Net.WebClient()) {
                byte[] l_Bytes = await l_Client.DownloadDataTaskAsync(new Uri(p_Url));

                l_NewTexture.LoadImage(l_Bytes, false);
            }
        }
        catch {
            l_NewTexture = resources.GsWhiteLogoTexture;
        }

        return l_NewTexture;
    }
    
    public static Texture2D MakeCorrespondHeight(Texture2D p_Texture, Rect p_Rect)
    {
        if (p_Rect.y >= p_Texture.height) return p_Texture;

        var l_Texture = p_Texture.GetPixels(0, (int)(p_Texture.width - p_Rect.y) / 2, p_Texture.width, (int)p_Rect.height);

        // ReSharper disable once PossibleLossOfFraction
        var l_ResultTexture = new Texture2D(p_Texture.width, (int)p_Rect.height);
        l_ResultTexture.SetPixels(l_Texture);
        l_ResultTexture.Apply();

        return l_ResultTexture;
    }

    public static async Task<Texture2D> Gradient(Texture2D p_Texture, Color p_Color1, Color p_Color2, EGradientDirection p_Direction = EGradientDirection.Horizontal, bool p_Invert = false, bool p_UseAlpha = false)
    {
        var l_Origin = p_Texture;

        var l_FirstColor  = p_Invert ? p_Color1 : p_Color2;
        var l_SecondColor = p_Invert ? p_Color2 : p_Color1;
        await Task.Run(() =>
        {
            for (var l_X = 0; l_X < p_Texture.width; l_X++)
            {
                for (var l_Y = 0; l_Y < p_Texture.height; l_Y++)
                {
                    var l_CurrentPixel = l_Origin.GetPixel(l_X, l_Y);

                    var l_Color2Multiplier = (p_Direction == EGradientDirection.Horizontal ? l_X : l_Y) / (float)(p_Direction == EGradientDirection.Horizontal ? l_Origin.width : l_Origin.height);

                    var l_Alpha = p_UseAlpha ? (l_FirstColor.a + l_SecondColor.a) / 2 * l_Color2Multiplier : l_Origin.GetPixel(l_X, l_Y).a;

                    l_Origin.SetPixel(
                        l_X, l_Y,
                        new Color(
                            l_CurrentPixel.r * ((l_FirstColor.r + l_SecondColor.r * l_Color2Multiplier) / 2),
                            l_CurrentPixel.g * ((l_FirstColor.g + l_SecondColor.g * l_Color2Multiplier) / 2),
                            l_CurrentPixel.b * ((l_FirstColor.b + l_SecondColor.b * l_Color2Multiplier) / 2),
                            l_CurrentPixel.a * l_Alpha)
                    );
                }
            }
        });
        l_Origin.Apply();

        return l_Origin;
    }

    public static async Task<Texture2D> AddOffset(Texture2D p_Origin, int p_Offset)
    {
        var l_Height = p_Origin.height - p_Offset * 2;

        if (l_Height <= 0)
        {
            l_Height = p_Origin.height;
        }

        var l_Result = new Texture2D(p_Origin.width, l_Height);

        await Task.Run(() =>
        {
            var l_Colors = p_Origin.GetPixels();

            for (var l_X = 0; l_X < l_Result.width; l_X++)
            {
                for (var l_Y = 0; l_Y < l_Result.height; l_Y++)
                {
                    var l_FixedY = l_Y + p_Offset;
                    if (l_Result.height == p_Origin.height)
                        l_FixedY = l_Y;

                    l_Result.SetPixel(l_X, l_Y, p_Origin.GetPixel(l_X, l_FixedY));
                }
            }
        });
        l_Result.Apply();
        return l_Result;
    }

    public static void RoundTexture(Texture2D self, float radius)
    {
        for (var corner = 0; corner < 4; corner++)
        for (var x = 0; x < radius; x++)
        {
            for (var y = 0; y < radius; y++)
            {
                var point = corner switch
                {
                    0 => new Vector2(x, y) /* Corner Bottom Left */,
                    1 => new Vector2(x + (self.width - radius), y) /* Corner Bottom Right */,
                    2 => new Vector2(x, y + (self.height - radius)) /* Corner Top Left */,
                    3 => new Vector2(x + (self.width - radius), y + (self.height - radius)) /* Corner Top Right */,
                    _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
                };
                var pointRadius = corner switch
                {
                    0 => new Vector2(radius, radius) /* Corner Bottom Left */,
                    1 => new Vector2(self.width - radius, radius) /* Corner Bottom Right */,
                    2 => new Vector2(radius, self.height - radius) /* Corner Top Left */,
                    3 => new Vector2(self.width - radius, self.height - radius) /* Corner Top Right */,
                    _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
                };
                var pixelColor = self.GetPixel((int)point.x, (int)point.y);
                if (Vector2.Distance(point, pointRadius) > radius)
                {
                    self.SetPixel((int)point.x, (int)point.y, m_TransparentColor);
                }

                // This logic seems weird but if it works, then it works.
                var (newX, newY) = corner switch
                {
                    0 => (point.x + radius, point.y + radius) /* Corner Bottom Left */,
                    1 => (self.width - 2f * radius + point.x, 2f * radius - point.y) /* Corner Bottom Right */,
                    2 => (2f * radius - point.x, self.height - 2f * radius + point.y) /* Corner Top Left */,
                    3 => (self.width - 2f * radius + point.x, self.height - 2f * radius + point.y) /* Corner Top Right */,
                    _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
                };
                
                self.SetPixel((int)newX, (int)newY, pixelColor.ColorWithAlpha(1));
            }
        }
    }
    public static async Task<Texture2D> CreateRoundedTextureAsync(
        Texture2D origin, float radius)
    {
        var texture = origin.GetCopy();
        await Task.Run(() => RoundTexture(texture, radius));
        texture.Apply();
        return texture;
    }
    public static Texture2D GetCopy(this Texture2D p_Texture)
    {
        var l_New  = new Texture2D(p_Texture.width, p_Texture.height);
        var l_Olds = p_Texture.GetPixels();
        l_New.SetPixels(l_Olds);
        return l_New;
    }

    public static FixedHeight GetHeight(int p_ImageViewWidth, int p_ImageViewHeight, int p_TextureWidth, int p_TextureHeigth)
    {
        if (p_ImageViewHeight > p_TextureHeigth)
        {
            var l_NonModified = new FixedHeight();
            l_NonModified.NewHeight = p_TextureHeigth;
            l_NonModified.TextureOffset = 0;
            return l_NonModified;
        }

        var l_WantedImageSize = (int)(p_TextureWidth * ((float)p_ImageViewHeight / p_ImageViewWidth));
        var l_FixedHeigth     = l_WantedImageSize;
        var l_Result          = new FixedHeight();
        l_Result.NewHeight     = l_FixedHeigth;
        l_Result.TextureOffset = p_TextureHeigth / 2 - l_WantedImageSize / 2;
        return l_Result;
    }

    public static async Task<Texture2D> CreateFlatTexture(int p_Width, int p_Height, Color p_Color)
    {
        var l_Texture = new Texture2D(p_Width, p_Height, TextureFormat.RGBA64, false);

        await Task.Run(() =>
        {
            for (var l_X = 0; l_X < p_Width; l_X++)
            {
                for (var l_Y = 0; l_Y < p_Height; l_Y++)
                {
                    l_Texture.SetPixel(l_X, l_Y, p_Color);
                }
            }
        });
        l_Texture.Apply();
        return l_Texture;
    }

    public static async Task<Texture2D> AddLine(Texture2D p_Texture, int p_Width, int p_Height, int p_PosX, int p_PosY, Color p_LineColor)
    {
        await Task.Run(() =>
        {
            for (var l_X = 0; l_X < p_Width; l_X++)
            {
                for (var l_Y = 0; l_Y < p_Height; l_Y++)
                {
                    p_Texture.SetPixel(l_X + p_PosX, l_Y + p_PosY, p_LineColor);
                }
            }
        });
        p_Texture.Apply();

        return p_Texture;
    }

    public struct FixedHeight
    {
        public int NewHeight;
        public int Position;
        public int TextureOffset;
    }
}