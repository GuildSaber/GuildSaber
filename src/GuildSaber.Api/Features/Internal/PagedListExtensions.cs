using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Internal;

public static class PagedListExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync();

        return new PagedList<T>(data, page, pageSize, totalCount);
    }
}