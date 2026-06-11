using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Shared;
using ERankedScoreType = GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests.ERankedScoreType;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapRequests;
using static GuildSaber.Api.Features.RankedMaps.Http.RankedMapResponses;

namespace GuildSaber.CSharpClient.Routes.RankedMaps;

/// <summary>
/// Client for interacting with ranked map endpoints.
/// </summary>
public class RankedMapClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private static Uri GetRankedMapUrl(
        ContextId contextId, Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> requestOptions)
        => new(
            $"contexts/{contextId}/ranked-maps?{(requestFilters.Search is null ? "" : $"search={requestFilters.Search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
            $"{(requestFilters.RankedScoreTypes is ERankedScoreType.None ? "" : $"&rankedScoreTypes={(int)requestFilters.RankedScoreTypes}")}" +
            $"{(!requestFilters.IncludeMapsWithoutScore ? "" : $"&includeMapsWithoutScore={requestFilters.IncludeMapsWithoutScore}")}" +
            $"{(requestFilters.DifficultyStarFrom is null ? "" : $"&difficultyStarFrom={requestFilters.DifficultyStarFrom}")}" +
            $"{(requestFilters.AccuracyStarFrom is null ? "" : $"&accuracyStarFrom={requestFilters.AccuracyStarFrom}")}" +
            $"{(requestFilters.DifficultyStarTo is null ? "" : $"&difficultyStarTo={requestFilters.DifficultyStarTo}")}" +
            $"{(requestFilters.AccuracyStarTo is null ? "" : $"&accuracyStarTo={requestFilters.AccuracyStarTo}")}" +
            $"{(requestFilters.DurationSecFrom is null ? "" : $"&durationSecFrom={requestFilters.DurationSecFrom}")}" +
            $"{(requestFilters.DurationSecTo is null ? "" : $"&durationSecTo={requestFilters.DurationSecTo}")}" +
            $"{(requestFilters.BpmFrom is null ? "" : $"&bpmFrom={requestFilters.BpmFrom}")}" +
            $"{(requestFilters.BpmTo is null ? "" : $"&bpmTo={requestFilters.BpmTo}")}" +
            $"{(requestFilters.NeedConfirmation is null ? "" : $"&needConfirmation={requestFilters.NeedConfirmation}")}" +
            $"{(requestFilters.CategoryIds is null ? "" : $"&categoryIds={string.Join(",", requestFilters.CategoryIds)}")}" +
            $"&matchAnyCategory={requestFilters.MatchAnyCategory}",
            UriKind.Relative
        );

    private static Uri GetRankedMapWithScoresUrl(
        ContextId contextId, PlayerId? playerId, Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> requestOptions)
        => new(
            $"contexts/{contextId}/ranked-maps/with-scores/{(playerId is null ? "@me" : playerId)}?{(requestFilters.Search is null ? "" : $"search={requestFilters.Search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
            $"{(requestFilters.RankedScoreTypes is ERankedScoreType.None ? "" : $"&rankedScoreTypes={(int)requestFilters.RankedScoreTypes}")}" +
            $"{(!requestFilters.IncludeMapsWithoutScore ? "" : $"&includeMapsWithoutScore={requestFilters.IncludeMapsWithoutScore}")}" +
            $"{(requestFilters.DifficultyStarFrom is null ? "" : $"&difficultyStarFrom={requestFilters.DifficultyStarFrom}")}" +
            $"{(requestFilters.AccuracyStarFrom is null ? "" : $"&accuracyStarFrom={requestFilters.AccuracyStarFrom}")}" +
            $"{(requestFilters.DifficultyStarTo is null ? "" : $"&difficultyStarTo={requestFilters.DifficultyStarTo}")}" +
            $"{(requestFilters.AccuracyStarTo is null ? "" : $"&accuracyStarTo={requestFilters.AccuracyStarTo}")}" +
            $"{(requestFilters.DurationSecFrom is null ? "" : $"&durationSecFrom={requestFilters.DurationSecFrom}")}" +
            $"{(requestFilters.DurationSecTo is null ? "" : $"&durationSecTo={requestFilters.DurationSecTo}")}" +
            $"{(requestFilters.BpmFrom is null ? "" : $"&bpmFrom={requestFilters.BpmFrom}")}" +
            $"{(requestFilters.BpmTo is null ? "" : $"&bpmTo={requestFilters.BpmTo}")}" +
            $"{(requestFilters.NeedConfirmation is null ? "" : $"&needConfirmation={requestFilters.NeedConfirmation}")}" +
            $"{(requestFilters.CategoryIds is null ? "" : $"&categoryIds={string.Join(",", requestFilters.CategoryIds)}")}" +
            $"&matchAnyCategory={requestFilters.MatchAnyCategory}",
            UriKind.Relative
        );

    /// <summary>
    /// Gets a ranked map by its ID.
    /// </summary>
    /// <param name="rankedMapId">The ID of the ranked map to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the ranked map if found, or null if not found.</returns>
    public async Task<Result<RankedMap?>> GetByIdAsync(int rankedMapId, CancellationToken cancellationToken = default)
        => await httpClient.GetAsync($"ranked-maps/{rankedMapId}", cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<RankedMap?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedMap?>(
                        $"Failed to retrieve ranked map with ID {rankedMapId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<RankedMap>(jsonOptions, cancellationToken: cancellationToken))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked maps for a specific context with optional search filtering.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked maps.</returns>
    public async Task<Result<PagedList<RankedMap>>> GetAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetRankedMapUrl(contextId, requestFilters, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedMap>>(
                        $"Failed to retrieve ranked maps for context ID {contextId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedMap>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked maps with point scores for a specific player.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked maps with score.</returns>
    public async Task<Result<PagedList<RankedMapWithScores>>> GetWithScoresAsync(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetRankedMapWithScoresUrl(contextId, playerId, requestFilters, requestOptions),
                    token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedMapWithScores>>(
                        $"Failed to retrieve ranked maps with point scores for context ID {contextId} and player {playerId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedMapWithScores>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked maps with point scores for the current user (@me).
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked maps with score for @me.</returns>
    public async Task<Result<PagedList<RankedMapWithScores>>> GetWithScoresAtMeAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                GetRankedMapWithScoresUrl(contextId, null, requestFilters, requestOptions))
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedMapWithScores>>(
                        $"Failed to retrieve ranked maps with point score for context ID {contextId} (@me), status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedMapWithScores>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Asynchronously retrieves ranked maps with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of <see cref="RankedMap" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of ranked maps when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving an empty array or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<RankedMap[]>> GetAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> pageOptions)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetRankedMapUrl(contextId, requestFilters, pageOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedMap[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedMap[]>(
                        $"Failed to retrieve ranked maps for context ID {contextId} at page {pageOptions.Page}" +
                        $": {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedMap>>(jsonOptions))
                    .Map(RankedMap[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked maps with point scores for a specific player, with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedMapResponses.RankedMapWithScores" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedMapWithScores[]>> GetAsyncWithScoresEnumerable(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> pageOptions)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetRankedMapWithScoresUrl(contextId, playerId, requestFilters, pageOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedMapWithScores[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedMapWithScores[]>(
                        $"Failed to retrieve ranked maps with point scores for context ID {contextId} and player {playerId} at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedMapWithScores>>(jsonOptions))
                    .Map(RankedMapWithScores[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked maps with scores for the current user (@me), with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked map request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedMapResponses.RankedMapWithScores" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedMapWithScores[]>> GetAsyncWithScoresAtMeEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedMapSorter> pageOptions)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetRankedMapWithScoresUrl(contextId, null, requestFilters, pageOptions);
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { Authorization = authenticationHeader }
            }).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedMapWithScores[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedMapWithScores[]>(
                        $"Failed to retrieve ranked maps with scores for context ID {contextId} (@me) at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content.ReadFromJsonAsync<PagedList<RankedMapWithScores>>(jsonOptions))
                    .Map(RankedMapWithScores[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}