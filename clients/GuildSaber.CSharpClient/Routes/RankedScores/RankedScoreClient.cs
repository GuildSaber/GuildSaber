using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Shared;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreRequests;
using static GuildSaber.Api.Features.RankedScores.Http.RankedScoreResponses;

namespace GuildSaber.CSharpClient.Routes.RankedScores;

/// <summary>
/// Client for interacting with ranked score endpoints.
/// </summary>
public class RankedScoreClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private static Uri GetRankedScoreUrl(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions)
        => new(
            $"contexts/{contextId}/ranked-scores?{(requestFilters.RankedScoreTypes is ERankedScoreType.None ? "" : $"rankedScoreTypes={(int)requestFilters.RankedScoreTypes}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
            $"{(requestFilters.DifficultyStarFrom is null ? "" : $"&difficultyStarFrom={requestFilters.DifficultyStarFrom}")}" +
            $"{(requestFilters.AccuracyStarFrom is null ? "" : $"&accuracyStarFrom={requestFilters.AccuracyStarFrom}")}" +
            $"{(requestFilters.DifficultyStarTo is null ? "" : $"&difficultyStarTo={requestFilters.DifficultyStarTo}")}" +
            $"{(requestFilters.AccuracyStarTo is null ? "" : $"&accuracyStarTo={requestFilters.AccuracyStarTo}")}" +
            $"{(requestFilters.BpmFrom is null ? "" : $"&bpmFrom={requestFilters.BpmFrom}")}" +
            $"{(requestFilters.BpmTo is null ? "" : $"&bpmTo={requestFilters.BpmTo}")}",
            UriKind.Relative
        );

    private static Uri GetPlayerRankedScoreUrl(
        ContextId contextId,
        PlayerId? playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions)
        => new(
            $"players/{(playerId is null ? "@me" : playerId)}/contexts/{contextId}/ranked-scores?{(requestFilters.RankedScoreTypes is ERankedScoreType.None ? "" : $"rankedScoreTypes={(int)requestFilters.RankedScoreTypes}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
            $"{(requestFilters.DifficultyStarFrom is null ? "" : $"&difficultyStarFrom={requestFilters.DifficultyStarFrom}")}" +
            $"{(requestFilters.AccuracyStarFrom is null ? "" : $"&accuracyStarFrom={requestFilters.AccuracyStarFrom}")}" +
            $"{(requestFilters.DifficultyStarTo is null ? "" : $"&difficultyStarTo={requestFilters.DifficultyStarTo}")}" +
            $"{(requestFilters.AccuracyStarTo is null ? "" : $"&accuracyStarTo={requestFilters.AccuracyStarTo}")}" +
            $"{(requestFilters.BpmFrom is null ? "" : $"&bpmFrom={requestFilters.BpmFrom}")}" +
            $"{(requestFilters.BpmTo is null ? "" : $"&bpmTo={requestFilters.BpmTo}")}",
            UriKind.Relative
        );

    private static Uri GetPlayerRankedScoreWithRankedMapUrl(
        ContextId contextId,
        PlayerId? playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions)
        => new(
            $"players/{(playerId is null ? "@me" : playerId)}/contexts/{contextId}/ranked-scores-with-ranked-map?{(requestFilters.RankedScoreTypes is ERankedScoreType.None ? "" : $"rankedScoreTypes={(int)requestFilters.RankedScoreTypes}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}" +
            $"{(requestFilters.DifficultyStarFrom is null ? "" : $"&difficultyStarFrom={requestFilters.DifficultyStarFrom}")}" +
            $"{(requestFilters.AccuracyStarFrom is null ? "" : $"&accuracyStarFrom={requestFilters.AccuracyStarFrom}")}" +
            $"{(requestFilters.DifficultyStarTo is null ? "" : $"&difficultyStarTo={requestFilters.DifficultyStarTo}")}" +
            $"{(requestFilters.AccuracyStarTo is null ? "" : $"&accuracyStarTo={requestFilters.AccuracyStarTo}")}" +
            $"{(requestFilters.BpmFrom is null ? "" : $"&bpmFrom={requestFilters.BpmFrom}")}" +
            $"{(requestFilters.BpmTo is null ? "" : $"&bpmTo={requestFilters.BpmTo}")}",
            UriKind.Relative
        );

    /// <summary>
    /// Gets a paginated list of ranked scores with ranked maps for a specific context.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores with ranked maps.</returns>
    public async Task<Result<PagedList<RankedScoreWithRankedMap>>> GetAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetRankedScoreUrl(contextId, requestFilters, requestOptions), token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScoreWithRankedMap>>(
                        $"Failed to retrieve ranked scores for context ID {contextId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked scores for a specific player.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores.</returns>
    public async Task<Result<PagedList<RankedScore>>> GetPlayerAsync(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetPlayerRankedScoreUrl(contextId, playerId, requestFilters, requestOptions),
                    token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScore>>(
                        $"Failed to retrieve ranked scores for context ID {contextId} and player {playerId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScore>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked scores for the current user (@me).
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores for @me.</returns>
    public async Task<Result<PagedList<RankedScore>>> GetPlayerAtMeAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                GetPlayerRankedScoreUrl(contextId, null, requestFilters, requestOptions))
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScore>>(
                        $"Failed to retrieve ranked scores for context ID {contextId} (@me), status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScore>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked scores with ranked maps for a specific player.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores with ranked maps.</returns>
    public async Task<Result<PagedList<RankedScoreWithRankedMap>>> GetPlayerWithRankedMapAsync(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(
                    GetPlayerRankedScoreWithRankedMapUrl(contextId, playerId, requestFilters, requestOptions),
                    token)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScoreWithRankedMap>>(
                        $"Failed to retrieve ranked scores with ranked maps for context ID {contextId} and player {playerId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets a paginated list of ranked scores with ranked maps for the current user (@me).
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked scores with ranked maps for @me.</returns>
    public async Task<Result<PagedList<RankedScoreWithRankedMap>>> GetPlayerWithRankedMapAtMeAsync(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                GetPlayerRankedScoreWithRankedMapUrl(contextId, null, requestFilters, requestOptions))
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PagedList<RankedScoreWithRankedMap>>(
                        $"Failed to retrieve ranked scores with ranked maps for context ID {contextId} (@me), status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Sets all pending-compatible ranked scores for an underlying score as confirmed.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="scoreId">The underlying score identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated ranked scores, or null if none were found.</returns>
    public async Task<Result<RankedScore[]?>> SetConfirmedAsync(
        ContextId contextId,
        ScoreId scoreId,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"contexts/{contextId}/ranked-scores/scores/{scoreId}/set-confirmed")
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<RankedScore[]?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScore[]?>(
                        $"Failed to confirm ranked scores from score ID {scoreId} for context ID {contextId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<RankedScore[]>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Sets all pending-compatible ranked scores for an underlying score as refused.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="scoreId">The underlying score identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated ranked scores, or null if none were found.</returns>
    public async Task<Result<RankedScore[]?>> SetRefusedAsync(
        ContextId contextId,
        ScoreId scoreId,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"contexts/{contextId}/ranked-scores/scores/{scoreId}/set-refused")
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<RankedScore[]?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScore[]?>(
                        $"Failed to refuse ranked scores from score ID {scoreId} for context ID {contextId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<RankedScore[]>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Reverts all pending-compatible ranked scores for an underlying score back to pending.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="scoreId">The underlying score identifier.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated ranked scores, or null if none were found.</returns>
    public async Task<Result<RankedScore[]?>> RevertToPendingAsync(
        ContextId contextId,
        ScoreId scoreId,
        CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post,
                $"contexts/{contextId}/ranked-scores/scores/{scoreId}/revert-to-pending")
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<RankedScore[]?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScore[]?>(
                        $"Failed to revert ranked scores from score ID {scoreId} for context ID {contextId} to pending, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<RankedScore[]>(jsonOptions, cancellationToken: token))
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Asynchronously retrieves ranked scores with ranked maps with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedScoreWithRankedMap" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of ranked scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving an empty array or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<RankedScoreWithRankedMap[]>> GetAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> pageOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetRankedScoreUrl(contextId, requestFilters, pageOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedScoreWithRankedMap[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScoreWithRankedMap[]>(
                        $"Failed to retrieve ranked scores for context ID {contextId} at page {pageOptions.Page}" +
                        $": {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, token))
                    .Map(RankedScoreWithRankedMap[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked scores for a specific player, with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedScore" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedScore[]>> GetPlayerAsyncEnumerable(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> pageOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetPlayerRankedScoreUrl(contextId, playerId, requestFilters, pageOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedScore[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScore[]>(
                        $"Failed to retrieve ranked scores for context ID {contextId} and player {playerId} at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScore>>(jsonOptions, token))
                    .Map(RankedScore[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked scores for the current user (@me), with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedScore" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedScore[]>> GetPlayerAtMeAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> pageOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetPlayerRankedScoreUrl(contextId, null, requestFilters, pageOptions);
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedScore[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScore[]>(
                        $"Failed to retrieve ranked scores for context ID {contextId} (@me) at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content.ReadFromJsonAsync<PagedList<RankedScore>>(jsonOptions, token))
                    .Map(RankedScore[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked scores with ranked maps for a specific player, with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedScoreWithRankedMap" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedScoreWithRankedMap[]>> GetPlayerWithRankedMapAsyncEnumerable(
        ContextId contextId,
        PlayerId playerId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> pageOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetPlayerRankedScoreWithRankedMapUrl(contextId, playerId, requestFilters, pageOptions);
            var response = await httpClient.GetAsync(url, token).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedScoreWithRankedMap[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScoreWithRankedMap[]>(
                        $"Failed to retrieve ranked scores with ranked maps for context ID {contextId} and player {playerId} at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, token))
                    .Map(RankedScoreWithRankedMap[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranked scores with ranked maps for the current user (@me), with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="requestFilters">Filters to apply to the ranked score request.</param>
    /// <param name="pageOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing arrays of
    /// <see cref="RankedScoreWithRankedMap" />.
    /// </returns>
    public async IAsyncEnumerable<Result<RankedScoreWithRankedMap[]>> GetPlayerWithRankedMapAtMeAsyncEnumerable(
        ContextId contextId,
        Filters requestFilters,
        PaginatedRequestOptions<ERankedScoreSorter> pageOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (pageOptions.Page <= pageOptions.MaxPage)
        {
            var url = GetPlayerRankedScoreWithRankedMapUrl(contextId, null, requestFilters, pageOptions);
            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false);
            pageOptions.Page++;

            Result<RankedScoreWithRankedMap[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedScoreWithRankedMap[]>(
                        $"Failed to retrieve ranked scores with ranked maps for context ID {contextId} (@me) at page {pageOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<RankedScoreWithRankedMap>>(jsonOptions, token))
                    .Map(RankedScoreWithRankedMap[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}