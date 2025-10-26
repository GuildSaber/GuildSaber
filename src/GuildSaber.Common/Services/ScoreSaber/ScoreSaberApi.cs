using System.Net;
using System.Net.Http.Json;
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
    public record struct PaginatedRequestOptions<TSortBy>(
        int Page = 1,
        int PageSize = 8,
        int MaxPage = int.MaxValue,
        TSortBy SortBy = default
    ) where TSortBy : struct, Enum
    {
        public static readonly PaginatedRequestOptions<TSortBy> Default = new();
    }

    private Uri GetPlayerScoreUrl(ScoreSaberId playerId, PaginatedRequestOptions<PlayerScoresSortBy> requestOptions)
        => new(
            $"api/player/{playerId}/scores?page={requestOptions.Page}&limit={requestOptions.PageSize}" +
            $"&sort={requestOptions.SortBy.ToString().ToLower()}&withMetadata=true",
            UriKind.Relative
        );

    /// <summary>
    /// Asynchronously retrieves a player's full scores with customizable pagination and sorting.
    /// </summary>
    /// <param name="playerId">The ScoreSaber ID of the player whose scores to retrieve.</param>
    /// <param name="requestOptions">Pagination and sorting settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="PlayerScore" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of scores when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// - Null when the player doesn't exist (HTTP 404)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<PlayerScore[]?>> GetPlayerScores(
        ScoreSaberId playerId, PaginatedRequestOptions<PlayerScoresSortBy> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetPlayerScoreUrl(playerId, requestOptions);
            var response = await httpClient.GetAsync(url);
            requestOptions.Page++;

            Result<PlayerScore[]?> result;
            yield return result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<PlayerScore[]?>(null),
                { IsSuccessStatusCode: false } => Failure<PlayerScore[]?>(
                    $"Failed to retrieve scores of player {playerId} at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content.ReadFromJsonAsync<PlayerScoreCollection>(_jsonOptions))
                    .Map(PlayerScore[]? (parsed) => parsed is null ? [] : parsed.PlayerScores)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    public async Task<Result<LeaderboardInfo?>> GetLeaderboardInfoAsync(
        SongHash hash, EDifficulty difficulty, SSGameMode gameMode) => await httpClient
            .GetAsync($"api/leaderboard/by-hash/{hash}/info?difficulty={(int)difficulty}&gameMode={gameMode}") switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<LeaderboardInfo?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<LeaderboardInfo?>(
                    $"Failed to retrieve leaderboard info for hash {hash}, difficulty {difficulty}, game mode {gameMode}:" +
                    $" {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<LeaderboardInfo>(_jsonOptions))
        };
}