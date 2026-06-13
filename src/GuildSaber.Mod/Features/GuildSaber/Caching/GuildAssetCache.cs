using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuildSaber.Common.StrongTypes;
using GuildSaber.CSharpClient;
using GuildSaber.Mod.Helpers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuildSaber.Mod.Features.GuildSaber.Caching;

public sealed class GuildAssetCache(GuildSaberCacheStore cacheStore, GuildSaberClient client) : IDisposable
{
    private const string RoundedGuildIconCacheKeyPrefix = "rounded-guild-icon:";
    private const string RoundedCategoryIconCacheKeyPrefix = "rounded-category-icon:";

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
        $"{RoundedCategoryIconCacheKeyPrefix}{categoryId}", client.Categories.GetLogoUrl(categoryId));

    private async Task<Texture2D?> GetOrFetchCachedTextureAsync(string key, Uri uri)
    {
        var texture = await cacheStore.GetOrCreateAsync(key, () => FetchAndRoundTextureAsync(uri));
        if (texture == null) cacheStore.Remove(key);

        return texture;
    }

    private async Task<Texture2D?> FetchAndRoundTextureAsync(Uri uri)
    {
        var texture = new Texture2D(100, 100);
        try
        {
            var bytes = await client.HttpClient.GetByteArrayAsync(uri);
            texture.LoadImage(bytes, false);

            var roundedTexture = await TextureUtils.CreateRoundedTextureAsync(texture, texture.width * 0.2f);

            lock (_ownedTexturesLock)
            {
                _ownedTextures.Add(roundedTexture);
            }

            return roundedTexture;
        }
        catch
        {
            return null;
        }
        finally
        {
            Object.Destroy(texture);
        }
    }
}