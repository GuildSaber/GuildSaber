using GuildSaber.Api.Features.Internal;

namespace GuildSaber.CSharpClient.Routes.Internal;

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
    public static readonly PaginatedRequestOptions<TSortBy> Default = new();
}