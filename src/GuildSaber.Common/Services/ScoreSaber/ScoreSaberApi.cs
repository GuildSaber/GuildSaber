using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Common.Services.ScoreSaber;

public class ScoreSaberApi(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ScoreSaberIdJsonConverter(),
            new ScoreSaberScoreIdJsonConverter(),
            new SSLeaderboardIdJsonConverter(),
            new SongHashJsonConverter()
        }
    };

    /// <summary>
    /// Generic options for paginated requests with sorting and ordering capabilities.
    /// </summary>
    /// <typeparam name="TSortBy">The enum type defining available sort fields.</typeparam>
    /// <param name="SortDirection">The sort direction for endpoints that support it.</param>
    public record struct PaginatedRequestOptions<TSortBy>(
        int Page = 1,
        int PageSize = 8,
        int MaxPage = int.MaxValue,
        TSortBy SortBy = default,
        SortDirection SortDirection = SortDirection.Desc
    ) where TSortBy : struct, Enum
    {
        public static readonly PaginatedRequestOptions<TSortBy> Default = new();
    }

    private Uri GetPlayerScoreUrl(ScoreSaberId playerId, PaginatedRequestOptions<PlayerScoresSortBy> requestOptions)
        => new(
            $"api/v2/players/{playerId}/scores?page={requestOptions.Page}&limit={requestOptions.PageSize}" +
            $"&sort={JsonNamingPolicy.CamelCase.ConvertName(requestOptions.SortBy.ToString())}&personalBest=true",
            UriKind.Relative
        );

    private Uri GetLeaderboardScoresUrl(
        SSLeaderboardId leaderboardId, PaginatedRequestOptions<LeaderboardScoresSortBy> requestOptions)
        => new(
            $"api/v2/leaderboards/{leaderboardId}/scores?page={requestOptions.Page}&limit={requestOptions.PageSize}" +
            $"&sort={JsonNamingPolicy.CamelCase.ConvertName(requestOptions.SortBy.ToString())}" +
            $"&sortDirection={JsonNamingPolicy.CamelCase.ConvertName(requestOptions.SortDirection.ToString())}",
            UriKind.Relative
        );

    /// <summary>
    /// Asynchronously retrieves a player's full scores with customizable pagination and sorting.
    /// </summary>
    /// <param name="playerId">The ScoreSaber ID of the player whose scores to retrieve.</param>
    /// <param name="requestOptions">Pagination and sorting settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="PlayerScore" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// - Null when the player doesn't exist (HTTP 404)
    /// Enumeration stops automatically after the last page, or after receiving null, an empty array, or an error.
    /// ScoreSaber doesn't expose sortDirection for player scores. Use Recent or Oldest for chronological ordering.
    /// </remarks>
    public async IAsyncEnumerable<Result<PlayerScore[]?>> GetPlayerScores(
        ScoreSaberId playerId, PaginatedRequestOptions<PlayerScoresSortBy> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetPlayerScoreUrl(playerId, requestOptions);
            using var response = await httpClient.GetAsync(url, token);
            var result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<ScoreCollection<PlayerScore>?>(null),
                { IsSuccessStatusCode: false } => Failure<ScoreCollection<PlayerScore>?>(
                    $"Failed to retrieve scores of player {playerId} at page {requestOptions.Page}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(()
                    => response.Content.ReadFromJsonAsync<ScoreCollection<PlayerScore>>(_jsonOptions, token))
            };

            token.ThrowIfCancellationRequested();
            requestOptions.Page++;
            yield return result.Map(static page => page?.Data);

            if (!result.TryGetValue(out var page) || page is null or { Data: [] }
                                                  || requestOptions.Page > page.Metadata.TotalPages)
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves a leaderboard's scores with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="leaderboardId">The ScoreSaber ID of the leaderboard whose scores to retrieve.</param>
    /// <param name="requestOptions">Pagination and sorting settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no scores are available, including unknown leaderboard IDs (HTTP 2XX)
    /// - Null when the endpoint returns HTTP 404
    /// Enumeration stops automatically after the last page, or after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<Score[]?>> GetLeaderboardScores(
        SSLeaderboardId leaderboardId, PaginatedRequestOptions<LeaderboardScoresSortBy> requestOptions,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetLeaderboardScoresUrl(leaderboardId, requestOptions);
            using var response = await httpClient.GetAsync(url, token);
            var result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<ScoreCollection<Score>?>(null),
                { IsSuccessStatusCode: false } => Failure<ScoreCollection<Score>?>(
                    $"Failed to retrieve scores of leaderboard {leaderboardId} at page {requestOptions.Page}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content.ReadFromJsonAsync<ScoreCollection<Score>>(_jsonOptions, token))
            };

            token.ThrowIfCancellationRequested();
            requestOptions.Page++;
            yield return result.Map(static page => page?.Data);

            if (!result.TryGetValue(out var page)
                || page is null or { Data: [] }
                || requestOptions.Page > page.Metadata.TotalPages)
                yield break;
        }
    }

    public async Task<Result<Leaderboard?>> GetLeaderboardInfoAsync(
        SongHash hash, EDifficulty difficulty, SSGameMode gameMode, CancellationToken token = default)
        => await httpClient.GetAsync(
                $"api/v2/leaderboards/hash/{hash}/{Uri.EscapeDataString(gameMode)}/{(int)difficulty}", token)
            switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Leaderboard?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Leaderboard?>(
                        $"Failed to retrieve leaderboard info for hash {hash}, difficulty {difficulty}, game mode {gameMode}:" +
                        $" {statusCode} {reasonPhrase}"
                    ),
                var response => await Try(() => response.Content
                    .ReadFromJsonAsync<Leaderboard>(_jsonOptions, token), exception =>
                    $"Failed to retrieve leaderboard info for hash {hash}, difficulty {difficulty}, game mode {gameMode}: {exception.Message}")
            };

    public async Task<Result<bool>> PlayerExistsAsync(ScoreSaberId scoreSaberId, CancellationToken token = default)
        => await httpClient.GetAsync($"api/v2/players/{scoreSaberId}/basic", token) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success(false),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<bool>($"Failed to check existence of player {scoreSaberId}: {statusCode} {reasonPhrase}"),
            _ => Success(true)
        };
}