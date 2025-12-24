using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Internal;
using GuildSaber.CSharpClient.Routes.Internal;
using static GuildSaber.Api.Features.Guilds.GuildResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds;

/// <summary>
/// Client for interacting with guild endpoints.
/// </summary>
public sealed class GuildClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private Uri GetGuildsUrl(string? search, PaginatedRequestOptions<GuildRequests.EGuildSorter> requestOptions)
        => new(
            $"guilds?{(search is null ? "" : $"search={search}&")}page={requestOptions.Page}&pageSize={requestOptions.PageSize}" +
            $"&order={requestOptions.Order}&sortBy={requestOptions.SortBy}",
            UriKind.Relative
        );

    /// <summary>
    /// Gets a guild by its ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild to retrieve.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the guild if found, or null if not found.</returns>
    public async Task<Result<Guild?>> GetByIdAsync(GuildId guildId, CancellationToken token = default)
        => await httpClient.GetAsync($"guilds/{guildId}", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<Guild?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Guild?>(
                    $"Failed to retrieve guild with ID {guildId}, status code: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<Guild>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Gets extended guild information by its ID.
    /// </summary>
    /// <param name="guildId">The ID of the guild to retrieve.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the extended guild info if found, or null if not found.</returns>
    public async Task<Result<GuildExtended?>> GetExtendedByIdAsync(GuildId guildId, CancellationToken token = default)
        => await httpClient.GetAsync($"guilds/{guildId}/extended", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<GuildExtended?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<GuildExtended?>(
                    $"Failed to retrieve guild with ID {guildId}, status code: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<GuildExtended>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Gets a guild by its Discord guild ID.
    /// </summary>
    /// <param name="discordGuildId">The Discord guild ID to search for.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the guild if found, or null if not found.</returns>
    public async Task<Result<Guild?>> GetByDiscordIdAsync(ulong discordGuildId, CancellationToken token = default)
        => await httpClient.GetAsync($"guilds/by-discord-id/{discordGuildId}", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<Guild?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Guild?>(
                    $"Failed to retrieve guild with Discord ID {discordGuildId}, status code: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<Guild>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Gets a paginated list of guilds with optional search filtering.
    /// </summary>
    /// <param name="search">Optional search term to filter guilds by name.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of guilds.</returns>
    public async Task<Result<PagedList<Guild>>> GetAsync(
        string? search, PaginatedRequestOptions<GuildRequests.EGuildSorter> requestOptions,
        CancellationToken token = default)
        => await httpClient.GetAsync(GetGuildsUrl(search, requestOptions), token).ConfigureAwait(false) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PagedList<Guild>>(
                    $"Failed to retrieve guilds at page {requestOptions.Page}: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<PagedList<Guild>>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Asynchronously retrieves guilds with customizable pagination, sorting, and ordering.
    /// </summary>
    /// <param name="search">Optional search term to filter guilds by name.</param>
    /// <param name="requestOptions">Pagination, sorting, and ordering settings for the request.</param>
    /// <returns>
    /// An async enumerable sequence of <see cref="Result{T}" /> containing nullable arrays of
    /// <see cref="Guild" />.
    /// </returns>
    /// <remarks>
    /// Each successful result contains:
    /// - A page of guilds when data is available
    /// - An empty array when no more data is available (HTTP 2XX)
    /// Enumeration stops automatically after receiving null, an empty array, or an error.
    /// </remarks>
    public async IAsyncEnumerable<Result<Guild[]>> GetAsyncEnumerable(
        string? search,
        PaginatedRequestOptions<GuildRequests.EGuildSorter> requestOptions)
    {
        while (requestOptions.Page <= requestOptions.MaxPage)
        {
            var url = GetGuildsUrl(search, requestOptions);
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            requestOptions.Page++;

            Result<Guild[]> result;
            yield return result = response switch
            {
                { IsSuccessStatusCode: false } => Failure<Guild[]>(
                    $"Failed to retrieve guilds at page {requestOptions.Page - 1}" +
                    $": {response.StatusCode} {response.ReasonPhrase}"),
                _ => await Try(() => response.Content
                        .ReadFromJsonAsync<PagedList<Guild>>(jsonOptions))
                    .Map(Guild[] (parsed) => parsed.Data)
                    .ConfigureAwait(false)
            };

            if (result is { IsFailure: true } or { Value: null or [] })
                yield break;
        }
    }

    /// <summary>
    /// Sets or removes the Discord guild ID for a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to update.</param>
    /// <param name="discordGuildId">The Discord guild ID to set, or null to remove.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated guild.</returns>
    public async Task<Result<Guild>> SetDiscordGuildIdAsync(
        GuildId guildId, ulong? discordGuildId, CancellationToken token = default)
    {
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"guilds/{guildId}")
        {
            Headers = { Authorization = authenticationHeader },
            Content = new StringContent(
                $$"""
                  [{"op":
                      "{{(discordGuildId is null ? "remove" : "replace")}}",
                      "path":"/{{nameof(Guild.DiscordInfo)}}/{{nameof(GuildDiscordInfo.MainDiscordGuildId)}}",
                      "value":"{{discordGuildId}}"
                  }]
                  """, Encoding.UTF8, "application/json-patch+json"
            )
        };

        var response = await httpClient.SendAsync(request, token)
            .ConfigureAwait(false);

        return response switch
        {
            { StatusCode: HttpStatusCode.NotFound }
                => Failure<Guild>($"Guild with ID {guildId} not found"),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Guild>(
                    $"Failed to update guild {guildId}, status code: {(int)statusCode} ({reasonPhrase})"),
            _ => await Try(async () =>
            {
                var guild = await response.Content.ReadFromJsonAsync<Guild>(jsonOptions, cancellationToken: token)
                    .ConfigureAwait(false);
                return guild!;
            }).ConfigureAwait(false)
        };
    }
}