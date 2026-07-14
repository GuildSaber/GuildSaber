using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Leaderboards.Http;
using GuildSaber.Api.Shared;
using static GuildSaber.Api.Features.Leaderboards.Http.LeaderboardResponses;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses;

namespace GuildSaber.CSharpClient.Routes.Leaderboards;

/// <summary>
/// Client for interacting with leaderboard endpoints.
/// </summary>
public sealed class LeaderboardClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    private Uri GetRankedMapLeaderboardUrl(
        int contextId,
        int pointId,
        long rankedMapId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/ranked-maps/{rankedMapId}/leaderboard?" +
            $"{(requestFilters.Search is null ? "" : $"search={requestFilters.Search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetMemberPointStatLeaderboardUrl(
        int contextId,
        int pointId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/leaderboard?" +
            $"{(requestFilters.Search is null ? "" : $"search={requestFilters.Search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetMemberCategoryPointStatLeaderboardUrl(
        ContextId contextId,
        int pointId,
        CategoryId categoryId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/categories/{categoryId}/leaderboard?" +
            $"{(requestFilters.Search is null ? "" : $"search={requestFilters.Search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    /// <summary>
    /// Gets a paginated leaderboard for a specific ranked map.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="rankedMapId">The ranked map identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores with player information.</returns>
    public async Task<Result<PagedList<RankedScoreWithPlayer>>> GetRankedMapLeaderboardAsync(
        int contextId,
        int pointId,
        long rankedMapId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(
                    GetRankedMapLeaderboardUrl(contextId, pointId, rankedMapId, requestFilters, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScoreWithPlayer>>(
                        $"Failed to retrieve ranked map leaderboard for map {rankedMapId} at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithPlayer>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated member point stat leaderboard for a specific context and point type.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of member point stats.</returns>
    public async Task<Result<PagedList<MemberPointStat>>> GetMemberPointStatLeaderboardAsync(
        int contextId,
        int pointId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(
                    GetMemberPointStatLeaderboardUrl(contextId, pointId, requestFilters, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<MemberPointStat>>(
                        $"Failed to retrieve member point stat leaderboard at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated member point stat leaderboard for a specific category.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of member point stats for the specified category.</returns>
    public async Task<Result<PagedList<MemberPointStat>>> GetMemberCategoryPointStatLeaderboardAsync(
        ContextId contextId,
        int pointId,
        CategoryId categoryId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(
                    GetMemberCategoryPointStatLeaderboardUrl(
                        contextId, pointId, categoryId, requestFilters, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<MemberPointStat>>(
                        $"Failed to retrieve member category point stat leaderboard for category {categoryId} at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Asynchronously retrieves ranked map leaderboard entries with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="rankedMapId">The ranked map identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="RankedScoreWithPlayer" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of ranked scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<RankedScoreWithPlayer[]>> GetRankedMapLeaderboardAsyncEnumerable(
        int contextId,
        int pointId,
        long rankedMapId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetRankedMapLeaderboardUrl(contextId, pointId, rankedMapId, requestFilters, requestOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            requestOptions.Page++;

            Result<RankedScoreWithPlayer[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<RankedScoreWithPlayer[]>(
                    $"Failed to retrieve ranked map leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithPlayer>>(
                            jsonOptions, cancellationToken: token))
                    .Map(RankedScoreWithPlayer[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves member point stat leaderboard entries with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="MemberPointStat" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of member stats when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<MemberPointStat[]>> GetMemberPointStatLeaderboardAsyncEnumerable(
        int contextId,
        int pointId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetMemberPointStatLeaderboardUrl(contextId, pointId, requestFilters, requestOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            requestOptions.Page++;

            Result<MemberPointStat[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<MemberPointStat[]>(
                    $"Failed to retrieve member point stat leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions, cancellationToken: token))
                    .Map(MemberPointStat[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves member category point stat leaderboard entries with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="pointId">The point identifier.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="requestFilters">Filters to apply to the leaderboard request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="MemberPointStat" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of member stats when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<MemberPointStat[]>> GetMemberCategoryPointStatLeaderboardAsyncEnumerable(
        ContextId contextId,
        int pointId,
        CategoryId categoryId,
        LeaderboardRequests.Filters requestFilters,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetMemberCategoryPointStatLeaderboardUrl(
                contextId, pointId, categoryId, requestFilters, requestOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            requestOptions.Page++;

            Result<MemberPointStat[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<MemberPointStat[]>(
                    $"Failed to retrieve member category point stat leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions, cancellationToken: token))
                    .Map(MemberPointStat[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}