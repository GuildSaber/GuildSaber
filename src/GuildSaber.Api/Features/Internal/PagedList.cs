using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Api.Features.Internal;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
public readonly record struct PagedList<T>(
    List<T> Data,
    int Page = 1,
    int PageSize = 10,
    int TotalCount = 0
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page * PageSize < TotalCount;
}

public static class PagedListExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedList<T>(data, page, pageSize, totalCount);
    }
}