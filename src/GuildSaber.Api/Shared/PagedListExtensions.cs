using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Shared;

public static class PagedListExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken token = default)
    {
        var totalCount = await query.CountAsync(token);
        var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);

        return new PagedList<T>(data, page, pageSize, totalCount);
    }
}