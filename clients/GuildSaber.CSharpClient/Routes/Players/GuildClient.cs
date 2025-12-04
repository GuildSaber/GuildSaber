using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.Api.Features.Players;
using GuildSaber.CSharpClient.Auth;
using GuildSaber.CSharpClient.Routes.Internal;
using static GuildSaber.Api.Features.Players.PlayerResponses;

namespace GuildSaber.CSharpClient.Routes.Players;

public sealed class PlayerClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
{
    private Uri GetPlayersUrl(string? search, PaginatedRequestOptions<PlayerRequests.EPlayerSorter> requestOptions)
        => new(
            $"players?{(search is null ? "" : $"search={search}&")}page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    public async Task<Result<Player?>> GetByIdAsync(int playerId, CancellationToken token)
        => await httpClient.GetAsync($"player/{playerId}", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<Player?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Player?>(
                    $"Failed to retrieve player with ID {playerId}, status code: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<Player?>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    public async Task<Result<PlayerAtMe>> GetAtMeAsync(GuildSaberAuthentication auth, CancellationToken token)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "players/@me")
            {
                Headers = { Authorization = auth.ToAuthenticationHeader() }
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<PlayerAtMe>(
                        $"Failed to retrieve current player, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                    .ReadFromJsonAsync<PlayerAtMe>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
            };

    public async Task<Result<PagedList<Player>>> GetAsync(
        string? search, PaginatedRequestOptions<PlayerRequests.EPlayerSorter> requestOptions, CancellationToken token)
        => await httpClient.GetAsync(GetPlayersUrl(search, requestOptions), token).ConfigureAwait(false) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PagedList<Player>>(
                    $"Failed to retrieve Players at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<PagedList<Player>>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Asynchronously retrieves Players with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="search">Optional search term to filter Players by name.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="Player" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of Players when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<Player[]>> GetAsyncEnumerable(
        string? search,
        PaginatedRequestOptions<PlayerRequests.EPlayerSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetPlayersUrl(search, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<Player[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<Player[]>(
                    $"Failed to retrieve Players at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<Player>>(jsonOptions))
                    .Map(Player[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }
}