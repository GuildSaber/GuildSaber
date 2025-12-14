using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.Leaderboards;
using GuildSaber.CSharpClient.Routes.Internal;
using static GuildSaber.Api.Features.Leaderboards.LeaderboardResponses;
using static GuildSaber.Api.Features.RankedScores.RankedScoreResponses;

namespace GuildSaber.CSharpClient.Routes.Leaderboards;

public sealed class LeaderboardClient(
    HttpClient httpClient,
    JsonSerializerOptions jsonOptions)
{
    private Uri GetRankedMapLeaderboardUrl(
        int contextId,
        int pointId,
        long rankedMapId,
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/ranked-maps/{rankedMapId}/leaderboard?" +
            $"page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetMemberPointStatLeaderboardUrl(
        int contextId,
        int pointId,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/leaderboard?" +
            $"page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetMemberCategoryPointStatLeaderboardUrl(
        int contextId,
        int pointId,
        int categoryId,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
        => new(
            $"contexts/{contextId}/points/{pointId}/categories/{categoryId}/leaderboard?" +
            $"page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    public async Task<Result<PagedList<RankedScoreWithPlayer>>> GetRankedMapLeaderboardAsync(
        int contextId,
        int pointId,
        long rankedMapId,
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions,
        CancellationToken token)
        => await httpClient.GetAsync(
                    GetRankedMapLeaderboardUrl(contextId, pointId, rankedMapId, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScoreWithPlayer>>(
                        $"Failed to retrieve ranked map leaderboard for map {rankedMapId} at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithPlayer>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    public async Task<Result<PagedList<MemberPointStat>>> GetMemberPointStatLeaderboardAsync(
        int contextId,
        int pointId,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        CancellationToken token)
        => await httpClient.GetAsync(
                    GetMemberPointStatLeaderboardUrl(contextId, pointId, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<MemberPointStat>>(
                        $"Failed to retrieve member point stat leaderboard at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    public async Task<Result<PagedList<MemberPointStat>>> GetMemberCategoryPointStatLeaderboardAsync(
        int contextId,
        int pointId,
        int categoryId,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions,
        CancellationToken token)
        => await httpClient.GetAsync(
                    GetMemberCategoryPointStatLeaderboardUrl(contextId, pointId, categoryId, requestOptions), token)
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
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
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
        PaginatedRequestOptions<LeaderboardRequests.ERankedMapLeaderboardSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetRankedMapLeaderboardUrl(contextId, pointId, rankedMapId, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<RankedScoreWithPlayer[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<RankedScoreWithPlayer[]>(
                    $"Failed to retrieve ranked map leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithPlayer>>(jsonOptions))
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
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
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
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetMemberPointStatLeaderboardUrl(contextId, pointId, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<MemberPointStat[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<MemberPointStat[]>(
                    $"Failed to retrieve member point stat leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions))
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
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
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
        int contextId,
        int pointId,
        int categoryId,
        PaginatedRequestOptions<LeaderboardRequests.EMemberStatLeaderboardSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetMemberCategoryPointStatLeaderboardUrl(contextId, pointId, categoryId, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<MemberPointStat[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<MemberPointStat[]>(
                    $"Failed to retrieve member category point stat leaderboard at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<MemberPointStat>>(jsonOptions))
                    .Map(MemberPointStat[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}