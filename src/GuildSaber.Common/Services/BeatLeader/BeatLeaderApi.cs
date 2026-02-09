using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader;

public class BeatLeaderApi(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new BeatLeaderIdJsonConverter(),
            new BeatLeaderScoreIdJsonConverter(),
            new BLLeaderboardIdJsonConverter()
        }
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

    private Uri GetPlayerScoreCompactUrl(BeatLeaderId id, PaginatedRequestOptions<ScoresSortBy> requestOptions)
        => new(
            $"player/{id}/scores/compact?page={requestOptions.Page}&count={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetPlayerScoreUrl(BeatLeaderId id, PaginatedRequestOptions<ScoresSortBy> requestOptions)
        => new(
            $"player/{id}/scores?page={requestOptions.Page}&count={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    private Uri GetLeaderboardUrl(BLLeaderboardId id, PaginatedRequestOptions<LeaderboardSortBy> requestOptions)
        => new(
            $"leaderboard/scores/{id}?page={requestOptions.Page}&count={requestOptions.PageSize}" +
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
    public async IAsyncEnumerable<Result<CompactScoreResponse[]?>> GetPlayerScoresCompactAsyncEnumerable(
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
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<ResponseWithMetadata<CompactScoreResponse>>(_jsonOptions))
                    .Map(CompactScoreResponse[]? (parsed) => parsed is null ? [] : parsed.Data)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves a player's full scores with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="playerId">The BeatLeader ID of the player whose scores to retrieve.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="ScoreResponse" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// - Null when the player doesn't exist (HTTP 404)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<ScoreResponse[]?>> GetPlayerScoresAsyncEnumerable(
        BeatLeaderId playerId, PaginatedRequestOptions<ScoresSortBy> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetPlayerScoreUrl(playerId, requestOptions);
            var response = await httpClient.GetAsync(url);
            requestOptions.Page++;

            Result<ScoreResponse[]?> result;
            yield return result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<ScoreResponse[]?>(null),
                { IsSuccessStatusCode: false } => Failure<ScoreResponse[]?>(
                    $"Failed to retrieve scores of player {playerId} at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<ResponseWithMetadata<ScoreResponse>>(_jsonOptions))
                    .Map(ScoreResponse[]? (parsed) => parsed is null ? [] : parsed.Data)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }


    /// <summary>
    /// Asynchronously retrieves a player's profile from BeatLeader.
    /// </summary>
    /// <param name="playerId">The BeatLeader ID of the player to retrieve.</param>
    /// <remarks>
    /// The result will be:
    /// - Success with player data for a found player
    /// - Success with null when the player doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<PlayerResponseFull?>> GetPlayerProfileAsync(BeatLeaderId playerId)
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
    /// <remarks>
    /// The result will be:
    /// - Success with player data for a found player
    /// - Success with null when the player doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<PlayerResponseFullWithStats?>> GetPlayerProfileWithStatsAsync(BeatLeaderId playerId)
        => await httpClient.GetAsync($"player/{playerId}?stats=true") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<PlayerResponseFullWithStats?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PlayerResponseFullWithStats?>(
                    $"Failed to retrieve player profile with stats of player {playerId}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<PlayerResponseFullWithStats>(_jsonOptions))
        };

    /// <summary>
    /// Asynchronously retrieves score statistics for a specific score from BeatLeader.
    /// </summary>
    /// <param name="scoreId">The unique identifier of the score to retrieve statistics for.</param>
    /// <remarks>
    /// The result will be:
    /// - Success with score statistics for a found score
    /// - Success with null when the score doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<ScoreStatistics?>> GetScoreStatisticsAsync(BeatLeaderScoreId scoreId)
        => await httpClient.GetAsync($"https://cdn.scorestats.beatleader.com/{scoreId}.json") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<ScoreStatistics?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<ScoreStatistics?>(
                    $"Failed to retrieve score statistics for score {scoreId}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<ScoreStatistics>(_jsonOptions))
        };

    public async Task<Result<ExMachinaResponse?>> GetExMachinaStarRatingAsync(
        SongHash hash, EDifficulty difficulty, string gameMode)
        => await httpClient.GetAsync($"https://stage.api.beatleader.net/ppai2/{hash}/{gameMode}/{(int)difficulty}")
            switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<ExMachinaResponse?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<ExMachinaResponse?>(
                        $"Failed to retrieve ExMachina rating for song {hash} difficulty {difficulty} gameMode {gameMode}: {statusCode} {reasonPhrase}"
                    ),
                var response => await Try(() => response.Content
                    .ReadFromJsonAsync<ExMachinaResponse>(_jsonOptions))
            };

    /// <summary>
    /// Asynchronously retrieves all leaderboards for a specific song from BeatLeader.
    /// </summary>
    /// <param name="hash">The hash of the song to retrieve leaderboards for.</param>
    /// <remarks>
    /// The result will be:
    /// - Success with leaderboards data for a found song
    /// - Success with null when the song doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<LeaderboardsResponse?>> GetLeaderboardsAsync(SongHash hash)
        => await httpClient.GetAsync($"leaderboards/hash/{hash}") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<LeaderboardsResponse?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<LeaderboardsResponse?>(
                    $"Failed to retrieve leaderboards for song {hash}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<LeaderboardsResponse>(_jsonOptions))
        };

    /// <summary>
    /// Asynchronously retrieves a specific leaderboard with paginated scores from BeatLeader.
    /// </summary>
    /// <param name="leaderboardId">The BeatLeader leaderboard ID to retrieve.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <remarks>
    /// The result will be:
    /// - Success with leaderboard data for a found leaderboard
    /// - Success with null when the leaderboard doesn't exist (HTTP 404)
    /// - Failure with an error message for other HTTP errors
    /// </remarks>
    public async Task<Result<LeaderboardResponse?>> GetLeaderboardAsync(
        BLLeaderboardId leaderboardId, PaginatedRequestOptions<LeaderboardSortBy> requestOptions)
        => await httpClient.GetAsync(GetLeaderboardUrl(leaderboardId, requestOptions)) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => null,
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<LeaderboardResponse?>(
                    $"Failed to retrieve leaderboard for leaderboard {leaderboardId} with pagination: {statusCode} {reasonPhrase}"
                ),
            var response => (await Try(() => response.Content.ReadFromJsonAsync<LeaderboardResponse?>(_jsonOptions)))!
        };

    /// <summary>
    /// Asynchronously retrieves leaderboard data for a given leaderboard ID with customizable pagination, sorting, and
    /// ordering.
    /// </summary>
    /// <param name="leaderboardId">The BeatLeader leaderboard ID to retrieve.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable <see cref="LeaderboardResponse" />
    /// objects.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of leaderboard data when available
    /// - Null when the leaderboard doesn't exist (HTTP 404)
    /// - Null when no scores are available for the requested page (HTTP 2XX with empty data)
    /// - Failure with an error message for other HTTP errors
    /// Enumeration stops automatically after receiving null or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<LeaderboardResponse?>> GetLeaderboardAsyncEnumerable(
        BLLeaderboardId leaderboardId, PaginatedRequestOptions<LeaderboardSortBy> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetLeaderboardUrl(leaderboardId, requestOptions);
            var response = await httpClient.GetAsync(url);
            requestOptions.Page++;

            Result<LeaderboardResponse?> result;
            yield return result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<LeaderboardResponse?>(null),
                { IsSuccessStatusCode: false } => Failure<LeaderboardResponse?>(
                    $"Failed to retrieve leaderboard {leaderboardId} at page {requestOptions.Page - 1}: {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<LeaderboardResponse?>(_jsonOptions))
                    .Map(value => value?.Scores is null or [] ? null : value)
            };

            if (result is { IsFailure: true } or { Value: null })
                yield break;
        }
    }
}