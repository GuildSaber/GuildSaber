using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.Api.Features.Internal;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
public readonly record struct PagedList<T>(
    T[] Data,
    int Page = 1,
    int PageSize = 10,
    int TotalCount = 0
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page * PageSize < TotalCount;
}