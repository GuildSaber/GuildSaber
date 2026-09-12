using GuildSaber.Database.Contexts.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.Api.Features.Website.LinkPreviews;

internal sealed class RankedMapLinkPreviewService(
    IServiceScopeFactory scopeFactory,
    HybridCache cache)
{
    private const string CacheKeyPrefix = "RankedMapLinkPreview_";
    internal const int CacheDurationSeconds = 60 * 60;

    private static readonly HybridCacheEntryOptions _cacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(CacheDurationSeconds)
    };

    public ValueTask<RankedMapLinkPreview?> GetAsync(RankedMapId rankedMapId, CancellationToken cancellationToken)
        => cache.GetOrCreateAsync($"{CacheKeyPrefix}{rankedMapId}", (scopeFactory, rankedMapId),
            async static (state, token) =>
            {
                await using var scope = state.scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

                return await GetFromDatabaseAsync(dbContext, state.rankedMapId, token);
            },
            _cacheEntryOptions, cancellationToken: cancellationToken);

    private static Task<RankedMapLinkPreview?> GetFromDatabaseAsync(
        ServerDbContext dbContext, RankedMapId rankedMapId, CancellationToken cancellationToken) => (
            from version in dbContext.MapVersions
            join map in dbContext.RankedMaps on version.RankedMapId equals map.Id
            where version.RankedMapId == rankedMapId
            orderby version.Order, version.AddedAt
            select new RankedMapLinkPreview(
                map.Id,
                version.Song.Hash,
                version.Song.BeatSaverKey,
                version.Song.Info,
                version.Song.Stats,
                version.SongDifficulty.Difficulty,
                version.SongDifficulty.GameMode.Name,
                version.SongDifficulty.Stats,
                map.Rating,
                map.Categories
                    .OrderBy(category => category.Info.Name)
                    .Select(category => category.Info.Name)
                    .ToArray(),
                map.Requirements))
        .FirstOrDefaultAsync(cancellationToken);
}