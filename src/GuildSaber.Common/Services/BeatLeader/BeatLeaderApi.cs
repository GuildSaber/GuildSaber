using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader;

public class BeatLeaderApi(HttpClient httpClient)
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
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
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
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// - Null when the player doesn't exist (HTTP 404)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<CompactScoreResponse[]?>> GetPlayerScoresCompact(
        BeatLeaderId playerId, PaginatedRequestOptions<ScoresSortBy> requestOptions)
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

    /// <summary>
    /// Asynchronously retrieves a player's profile from BeatLeader.
    /// </summary>
    /// <param name="playerId">The BeatLeader ID of the player to retrieve.</param>
    /// <returns>
    /// A task representing the asynchronous operation that contains a <see cref="Result{T}" /> of a nullable
    /// <see cref="PlayerResponseFull" /> object.
    /// </returns>
    /// <remarks>
    /// The result will be:
    /// - Success with player data for a found player
    /// - Success with null when the player doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<PlayerResponseFull?>> GetPlayerProfile(BeatLeaderId playerId)
        => await httpClient.GetAsync($"player/{playerId}?stats=false") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<PlayerResponseFull?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PlayerResponseFull?>(
                    $"Failed to retrieve player profile of player {playerId}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content.ReadFromJsonAsync<PlayerResponseFull>(_jsonOptions))
        };

    /// <summary>
    /// Asynchronously retrieves a player's profile with detailed statistics from BeatLeader.
    /// </summary>
    /// <param name="playerId">The BeatLeader ID of the player to retrieve.</param>
    /// <returns>
    /// A task representing the asynchronous operation that contains a <see cref="Result{T}" /> of a nullable
    /// <see cref="PlayerResponseFullWithStats" /> object.
    /// </returns>
    /// <remarks>
    /// The result will be:
    /// - Success with player data for a found player
    /// - Success with null when the player doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<PlayerResponseFullWithStats?>> GetPlayerProfileWithStats(BeatLeaderId playerId)
        => await httpClient.GetAsync($"player/{playerId}?stats=true") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<PlayerResponseFullWithStats?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PlayerResponseFullWithStats?>(
                    $"Failed to retrieve player profile with stats of player {playerId}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(()
                => response.Content.ReadFromJsonAsync<PlayerResponseFullWithStats>(_jsonOptions))
        };
}