using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.OldGuildSaber.Models;
using GuildSaber.Common.Services.OldGuildSaber.Models.Responses;

namespace GuildSaber.Common.Services.OldGuildSaber;

public class OldGuildSaberApi(HttpClient httpClient)
{
    private const string ApiLink = "https://api.guildsaber.com/";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BeatSaverKeyJsonConverter(), new SongHashJsonConverter() }
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
        bool Reverse = false
    ) where TSortBy : struct, Enum
    {
        public static readonly PaginatedRequestOptions<TSortBy> Default = new();
    }

    private Uri GetRankedDifficultiesUrl(int guildId, PaginatedRequestOptions<RankedMapsSortBy> requestOptions)
        => new(
            $"{ApiLink}rankedmaps/data/all?guild-id={guildId}&page={requestOptions.Page}&countperpage={requestOptions.PageSize}" +
            $"&sort={(int)requestOptions.SortBy}&reverse={requestOptions.Reverse}",
            UriKind.Absolute
        );

    private Uri GetRankingLevelsUrl(int guildId, int? categoryId = null)
        => new(
            categoryId is null
                ? $"{ApiLink}levels/data/all?guild-id={guildId}"
                : $"{ApiLink}levels/data/all?guild-id={guildId}&category-id={categoryId}",
            UriKind.Absolute
        );

    /// <summary>
    /// Asynchronously retrieves ranked maps for a guild with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose ranked maps to retrieve.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="PagedRankedDifficulties.RankedMapData" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of ranked difficulties when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<PagedRankedDifficulties.RankedMapData[]?>> GetGuildRankedMaps(
        int guildId, PaginatedRequestOptions<RankedMapsSortBy> requestOptions)
    {
        var rateLimitRetries = 0;
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetRankedDifficultiesUrl(guildId, requestOptions);
            var response = await httpClient.GetAsync(url);
            if (response.StatusCode == (HttpStatusCode)429)
            {
                rateLimitRetries++;
                if (rateLimitRetries > 8)
                {
                    yield return Failure<PagedRankedDifficulties.RankedMapData[]?>(
                        $"Exceeded maximum retries due to rate limiting when retrieving ranked difficulties for guild {guildId} at page {requestOptions.Page}");
                    yield break;
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
                continue;
            }

            requestOptions.Page++;
            rateLimitRetries = 0;

            Result<PagedRankedDifficulties.RankedMapData[]?> result;
            yield return result = response switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<PagedRankedDifficulties.RankedMapData[]?>(null),
                { IsSuccessStatusCode: false } => Failure<PagedRankedDifficulties.RankedMapData[]?>(
                    $"Failed to retrieve ranked difficulties for guild {guildId} at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedRankedDifficulties>(_jsonOptions))
                    .Map(PagedRankedDifficulties.RankedMapData[]? (parsed) => parsed is null ? [] : parsed.RankedMaps)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Asynchronously retrieves ranking levels for a guild from OldGuildSaber.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose ranking levels to retrieve.</param>
    /// <param name="categoryId">Optional category ID to filter levels by category.</param>
    /// <returns>
    /// A <see cref="Result{T}" /> containing an array of <see cref="RankingLevel" />.
    /// </returns>
    /// <remarks>
    /// The result will be:
    /// - Success with an array of ranking levels when data is available
    /// - Success with an empty array when no levels exist for the guild
    /// - Failure with an error message for HTTP errors
    /// </remarks>
    public async Task<Result<RankingLevel[]>> GetRankingLevelsAsync(int guildId, int? categoryId = null)
        => await httpClient.GetAsync(GetRankingLevelsUrl(guildId, categoryId)) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<RankingLevel[]>(
                    $"Failed to retrieve ranking levels for guild {guildId}: {statusCode} {reasonPhrase}"
                ),
            var response => await Try(() => response.Content
                    .ReadFromJsonAsync<RankingLevel[]>(_jsonOptions))
                .Map(levels => levels ?? [])
        };

    /// <summary>
    /// Asynchronously retrieves ranking categories for a guild from OldGuildSaber.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose ranking categories to retrieve.</param>
    /// <returns>
    /// A <see cref="Result{T}" /> containing an array of <see cref="RankingLevel" />.
    /// </returns>
    /// <remarks>
    /// The result will be:
    /// - Success with an array of ranking Categories when data is available
    /// - Success with an empty array when no levels exist for the guild
    /// - Failure with an error message for HTTP errors
    /// </remarks>
    public async Task<Result<RankingCategory[]>> GetRankingCategoriesAsync(int guildId)
        => await httpClient.GetAsync(new Uri($"{ApiLink}categories/data/all?guild-id={guildId}", UriKind.Absolute))
            switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<RankingCategory[]>(
                        $"Failed to retrieve ranking categories for guild {guildId}: {statusCode} {reasonPhrase}"
                    ),
                var response => await Try(() => response.Content
                        .ReadFromJsonAsync<RankingCategory[]>(_jsonOptions))
                    .Map(categories => categories ?? [])
            };
}