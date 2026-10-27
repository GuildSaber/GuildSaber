namespace GuildSaber.Api.Shared;

/// <summary>
/// Generic options for paginated requests with sorting and ordering capabilities.
/// </summary>
/// <typeparam name="TSortBy">The enum type defining available sort fields.</typeparam>
public record struct PaginatedRequestOptions<TSortBy>(
    int Page = 1,
    int PageSize = 8,
    int MaxPage = int.MaxValue,
    TSortBy SortBy = default,
    EOrder Order = EOrder.Desc
) where TSortBy : struct, Enum
{
    /// <remarks>Struct have a public parameterless constructor so we make sure it would follow the proper defaults on use.</remarks>
    public PaginatedRequestOptions() : this(Page: 1) { }
}