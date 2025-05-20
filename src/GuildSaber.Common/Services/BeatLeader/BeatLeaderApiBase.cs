using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;

namespace GuildSaber.Common.Services.BeatLeader;

public abstract class BeatLeaderApiBase(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Generic options for paginated requests with sorting and ordering capabilities.
    /// </summary>
    /// <typeparam name="TSortBy">The enum type defining available sort fields.</typeparam>
    public record struct PaginatedRequestOptions<TSortBy>(
        int Page = 1,
        int PageSize = 8,
        int MaxPage = int.MaxValue,
        TSortBy SortBy = default,
        Order Order = Order.Desc
    ) where TSortBy : struct, Enum
    {
        public static readonly PaginatedRequestOptions<TSortBy> Default = new();
    }

    private Uri GetPlayerScoreCompactUrl(ulong playerId, PaginatedRequestOptions<ScoresSortBy> requestOptions)
        => new(
            $"player/{playerId}/scores/compact?page={requestOptions.Page}&count={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sort={requestOptions.SortBy}",
            UriKind.Relative
        );

    /// <summary>
    /// Asynchronously retrieves a player's compact scores with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="playerId">The BeatLeader ID of the player whose scores to retrieve.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="CompactScoreResponse" />.
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// - Null when the player doesn't exist (HTTP 404)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </returns>
    protected async IAsyncEnumerable<Result<CompactScoreResponse[]?>> GetPlayerScoresCompact(
        ulong playerId, PaginatedRequestOptions<ScoresSortBy> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetPlayerScoreCompactUrl(playerId, requestOptions);
            var response = await httpClient.GetAsync(url);
            requestOptions.Page++;

            Result<CompactScoreResponse[]?> result;
            yield return result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<CompactScoreResponse[]?>(null),
                { IsSuccessStatusCode: false } => Failure<CompactScoreResponse[]?>(
                    $"Failed to retrieve compact scores of player {playerId} at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() =>
                        response.Content.ReadFromJsonAsync<ResponseWithMetadata<CompactScoreResponse>>(_jsonOptions))
                    .Map(CompactScoreResponse[]? (parsed) => parsed is null ? [] : parsed.Data)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}