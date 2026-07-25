using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B83.Image.GIF;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.GuildSaber.Caching;

public sealed class GuildAssetCache(
    GuildSaberCacheStore cacheStore,
    GuildSaberClient client,
    Logger logger) : IDisposable
{
    private const string RoundedGuildIconCacheKeyPrefix = "rounded-guild-icon:";
    private const string RoundedCategoryIconCacheKeyPrefix = "rounded-category-icon:";
    private const string PlayerAvatarCacheKeyPrefix = "player-avatar:";

    // MemoryCache only stores managed references; Unity textures still need Object.Destroy on dispose.
    private readonly List<Texture2D> _ownedTextures = [];
    private readonly object _ownedTexturesLock = new();

    public void Dispose()
    {
        Texture2D[] textures;
        lock (_ownedTexturesLock)
        {
            textures = _ownedTextures.Distinct().ToArray();
            _ownedTextures.Clear();
        }

        foreach (var texture in textures)
            if (texture != null)
                Object.Destroy(texture);
    }

    public Task<Texture2D?> GetOrFetchRoundedGuildIcon(GuildId guildId) => GetOrFetchCachedTextureAsync(
        $"{RoundedGuildIconCacheKeyPrefix}{guildId}", client.Guilds.GetLogoUrl(guildId));

    public Task<Texture2D?> GetOrFetchRoundedCategoryIcon(CategoryId categoryId) => GetOrFetchCachedTextureAsync(
        $"{RoundedCategoryIconCacheKeyPrefix}{categoryId}", client.Categories.GetLogoUrl(categoryId), round: true);

    public Task<Texture2D?> GetOrFetchPlayerAvatar(PlayerId playerId, string avatarUrl)
        => GetOrFetchCachedTextureAsync(
            $"{PlayerAvatarCacheKeyPrefix}{playerId}:{avatarUrl}",
            new Uri(avatarUrl),
            round: false);

    private async Task<Texture2D?> GetOrFetchCachedTextureAsync(string key, Uri uri, bool round = true)
    {
        var texture = await cacheStore.GetOrCreateAsync(key, () => FetchTextureAsync(uri, round));
        if (texture == null) cacheStore.Remove(key);

        return texture;
    }

    private async Task<Texture2D?> FetchTextureAsync(Uri uri, bool round)
    {
        Texture2D? texture = null;
        try
        {
            var bytes = await client.HttpClient.GetByteArrayAsync(uri);
            texture = IsGif(bytes) ? LoadGif(bytes) : LoadImage(bytes);

            if (texture is null)
                return null;

            var result = round
                ? await TextureUtils.CreateRoundedTextureAsync(texture, texture.width * 0.2f)
                : texture;

            lock (_ownedTexturesLock)
            {
                _ownedTextures.Add(result);
            }

            if (round) Object.Destroy(texture);
            return result;
        }
        catch (Exception exception)
        {
            if (texture != null) Object.Destroy(texture);
            logger.Warn($"Failed to load image from {uri}: {exception.Message}");
            return null;
        }
    }

    private static Texture2D? LoadImage(byte[] bytes)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(bytes, false)) return texture;

        Object.Destroy(texture);
        return null;
    }

    private static Texture2D? LoadGif(byte[] bytes)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var gif = new GIFLoader().Load(reader);
        if (gif.imageData.Count == 0 || gif.screen.width == 0 || gif.screen.height == 0)
            return null;

        var texture = new Texture2D(gif.screen.width, gif.screen.height, TextureFormat.RGBA32, false);
        var pixels = new Color32[texture.width * texture.height];
        gif.DrawImageTo(0, pixels, texture.width, texture.height);
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static bool IsGif(byte[] bytes)
        => bytes.Length >= 6
           && bytes[0] == 'G'
           && bytes[1] == 'I'
           && bytes[2] == 'F'
           && bytes[3] == '8'
           && bytes[5] == 'a'
           && bytes[4] is 0x37 or 0x39;
}