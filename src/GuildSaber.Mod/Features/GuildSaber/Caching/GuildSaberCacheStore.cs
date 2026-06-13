using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace GuildSaber.Mod.Features.GuildSaber.Caching;

public sealed class GuildSaberCacheStore : IDisposable
{
    private const string CacheName = "GuildSaber.Mod";
    private readonly MemoryCache _cache = new(CacheName);

    public void Dispose() => _cache.Dispose();

    public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        => GetOrCreateAsync(key, CreatePolicy(absoluteExpiration), factory);

    public async Task<T> GetOrCreateAsync<T>(string key, CacheItemPolicy policy, Func<Task<T>> factory)
    {
        var createdEntry = new Lazy<Task<T>>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
        var entry = _cache.AddOrGetExisting(key, createdEntry, policy) switch
        {
            null => createdEntry,
            Lazy<Task<T>> typedEntry => typedEntry,
            var existingEntry => throw new InvalidOperationException(
                $"Cache key '{key}' already contains a value of type '{existingEntry.GetType().FullName}'.")
        };

        try
        {
            return await entry.Value;
        }
        catch
        {
            RemoveIfCurrent(key, entry);
            throw;
        }
    }

    public void Remove(string key) => _cache.Remove(key);

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _cache.Select(x => x.Key).Where(x => x.StartsWith(prefix, StringComparison.Ordinal)))
            _cache.Remove(key);
    }

    private static CacheItemPolicy CreatePolicy(TimeSpan? absoluteExpirationRelativeToNow)
        => absoluteExpirationRelativeToNow is { } expiration
            ? new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(expiration) }
            : new CacheItemPolicy();

    private void RemoveIfCurrent<T>(string key, Lazy<Task<T>> entry)
    {
        if (ReferenceEquals(_cache.Get(key), entry))
            _cache.Remove(key);
    }
}