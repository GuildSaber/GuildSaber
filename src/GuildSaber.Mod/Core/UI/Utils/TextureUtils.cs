using System;
using System.Threading.Tasks;
using GuildSaber.Mod.Core.UI.Extensions;
using UnityEngine;

namespace GuildSaber.Mod.Core.UI.Utils;

// TODO: Turn this garbage into shader
internal static class TextureUtils
{
    private static readonly Color _transparentColor = new(0, 0, 0, 0);

    public enum EGradientDirection
    {
        Horizontal,
        Vertical
    }

    public struct FixedHeight
    {
        public int NewHeight;
        public int Position;
        public int TextureOffset;
    }

    //TODO: This function sucks because it creates a new texture instance only when the rect is out of bounds, so the
    // caller might get the same instance of the texture back. Ngl, mutating the original or explicitly returning a copy might be better.
    // I kinda prefer mutating the original as the caller can decide when to create copies and when not to.
    // Note: The original name was: "MakeCorrespondHeight", but it's cropping essentially. I guess this should just be removed altogether if unneeded anyway.
    public static Texture2D CropToHeight(Texture2D origin, Rect rect)
    {
        if (rect.y >= origin.height)
            return origin;

        var pixels = origin.GetPixels(0, (int)(origin.width - rect.y) / 2, origin.width, (int)rect.height);
        var result = new Texture2D(origin.width, (int)rect.height);

        result.SetPixels(pixels);
        result.Apply();

        return result;
    }

    public static async Task<Texture2D> Gradient(
        Texture2D origin, Color color1, Color color2, EGradientDirection direction = EGradientDirection.Horizontal,
        bool invert = false, bool useAlpha = false)
    {
        var firstColor = invert ? color1 : color2;
        var secondColor = invert ? color2 : color1;
        await Task.Run(() =>
        {
            for (var x = 0; x < origin.width; x++)
            for (var y = 0; y < origin.height; y++)
            {
                var currentPixel = origin.GetPixel(x, y);

                var color2Multiplier = direction switch
                {
                    EGradientDirection.Horizontal => x / (float)origin.width,
                    EGradientDirection.Vertical => y / (float)origin.height,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                };

                var alpha = useAlpha
                    ? (firstColor.a + secondColor.a) / 2 * color2Multiplier
                    : currentPixel.a;

                origin.SetPixel(
                    x, y,
                    new Color(
                        currentPixel.r * ((firstColor.r + secondColor.r * color2Multiplier) / 2),
                        currentPixel.g * ((firstColor.g + secondColor.g * color2Multiplier) / 2),
                        currentPixel.b * ((firstColor.b + secondColor.b * color2Multiplier) / 2),
                        currentPixel.a * alpha)
                );
            }
        });
        origin.Apply();

        return origin;
    }

    public static async Task<Texture2D> AddOffset(Texture2D origin, int offset)
    {
        var height = origin.height - offset * 2;
        if (height <= 0) height = origin.height;

        var result = new Texture2D(origin.width, height);
        await Task.Run(() =>
        {
            for (var x = 0; x < result.width; x++)
            for (var y = 0; y < result.height; y++)
            {
                var fixedY = y + offset;
                if (result.height == origin.height)
                    fixedY = y;

                result.SetPixel(x, y, origin.GetPixel(x, fixedY));
            }
        });

        result.Apply();
        return result;
    }

    public static async Task<Texture2D> RoundTextureAsync(Texture2D self, float radius)
    {
        var newTexture = self.GetCopy();
        await Task.Run(() =>
        {
            RoundTexture(newTexture, radius);
        });
        newTexture.Apply();
        
        return newTexture;
    }

    public static void RoundTexture(Texture2D self, float radius)
    {
        for (var corner = 0; corner < 4; corner++)
        for (var x = 0; x < radius; x++)
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
            
            if (Vector2.Distance(point, pointRadius) > radius)
                self.SetPixel((int)point.x, (int)point.y, _transparentColor);
        }
    }

    public static Texture2D GetCopy(this Texture2D texture)
    {
        var newTexture = new Texture2D(texture.width, texture.height);
        newTexture.SetPixels(texture.GetPixels());

        return newTexture;
    }

    public static FixedHeight GetHeight(int imageViewWidth, int imageViewHeight, int textureWidth, int textureHeight)
    {
        if (imageViewHeight > textureHeight)
        {
            var nonModified = new FixedHeight
            {
                NewHeight = textureHeight,
                TextureOffset = 0
            };

            return nonModified;
        }

        var wantedImageSize = (int)(textureWidth * ((float)imageViewHeight / imageViewWidth));
        return new FixedHeight
        {
            NewHeight = wantedImageSize,
            TextureOffset = textureHeight / 2 - wantedImageSize / 2
        };
    }

    public static async Task<Texture2D> CreateFlatTexture(int width, int height, Color color)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA64, false);
        await Task.Run(() =>
        {
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                texture.SetPixel(x, y, color);
        });

        texture.Apply();
        return texture;
    }

    public static async Task<Texture2D> AddLine(
        Texture2D texture, int width, int height, int posX, int posY, Color lineColor)
    {
        await Task.Run(() =>
        {
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                texture.SetPixel(x + posX, y + posY, lineColor);
        });

        texture.Apply();
        return texture;
    }
}