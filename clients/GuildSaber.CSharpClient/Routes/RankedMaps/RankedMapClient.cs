using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.CSharpClient.Routes.Internal;
using static GuildSaber.Api.Features.RankedMaps.RankedMapResponses;

namespace GuildSaber.CSharpClient.Routes.RankedMaps;

/// <summary>
/// Client for interacting with ranked map endpoints.
/// </summary>
public class RankedMapClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
{
    private Uri GetRankedMapUrl(
        int contextId, string? search, PaginatedRequestOptions<RankedMapRequest.ERankedMapSorter> requestOptions)
        => new(
            $"contexts/{contextId}/ranked-maps?{(search is null ? "" : $"search={search}&")}page={requestOptions.Page}" +
            $"&pageSize={requestOptions.PageSize}&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
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
    /// <param name="search">Optional search term to filter ranked maps.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of ranked maps.</returns>
    public async Task<Result<PagedList<RankedMap>>> GetAsync(
        int contextId,
        string? search,
        PaginatedRequestOptions<RankedMapRequest.ERankedMapSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetRankedMapUrl(contextId, search, requestOptions), token)
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
    /// Asynchronously retrieves ranked maps with automatic pagination.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="search">Optional search term to filter ranked maps.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
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
        int contextId,
        string? search,
        PaginatedRequestOptions<RankedMapRequest.ERankedMapSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetRankedMapUrl(contextId, search, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<RankedMap[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankedMap[]>(
                        $"Failed to retrieve ranked maps for context ID {contextId} at page {requestOptions.Page}" +
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
}