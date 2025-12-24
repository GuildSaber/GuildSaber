using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Api.Features.Internal;
using static GuildSaber.Api.Features.Guilds.Categories.CategoryResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Categories;

/// <summary>
/// Client for interacting with category endpoints.
/// </summary>
public sealed class CategoryClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private static Uri GetCategoriesUrl(int page, int pageSize)
        => new($"categories?page={page}&pageSize={pageSize}", UriKind.Relative);

    /// <summary>
    /// Gets a paginated list of all categories.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing a paginated list of categories.</returns>
    public async Task<Result<PagedList<Category>>> GetAsync(
        int page = 1, int pageSize = 10, CancellationToken token = default)
        => await httpClient.GetAsync(GetCategoriesUrl(page, pageSize), token).ConfigureAwait(false) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<PagedList<Category>>(
                    $"Failed to retrieve categories at page {page}: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<PagedList<Category>>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    /// <param name="categoryId">The ID of the category to retrieve.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the category if found, or null if not found.</returns>
    public async Task<Result<Category?>> GetByIdAsync(int categoryId, CancellationToken token = default)
        => await httpClient.GetAsync($"categories/{categoryId}", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<Category?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Category?>(
                    $"Failed to retrieve category with ID {categoryId}: {(int)statusCode} ({reasonPhrase})"),
            var response => await Try(() => response.Content
                .ReadFromJsonAsync<Category?>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
        };

    /// <summary>
    /// Gets all categories for a specific guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to retrieve categories for.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing an array of categories for the specified guild.</returns>
    public async Task<Result<Category[]>> GetAllByGuildIdAsync(GuildId guildId, CancellationToken token = default)
        => await httpClient.GetAsync($"guilds/{guildId}/categories", token).ConfigureAwait(false) switch
        {
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Category[]>(
                    $"Failed to retrieve categories for guild with ID {guildId}: {(int)statusCode} ({reasonPhrase})"),
            var response => (await Try(() => response.Content
                .ReadFromJsonAsync<Category[]>(jsonOptions, cancellationToken: token)).ConfigureAwait(false))!
        };

    /// <summary>
    /// Creates a new category for a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to create the category for.</param>
    /// <param name="request">The category creation request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the created category.</returns>
    public async Task<Result<Category>> CreateAsync(
        GuildId guildId, CategoryRequests.CreateCategory request, CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"guilds/{guildId}/categories")
            {
                Headers = { Authorization = authenticationHeader },
                Content = JsonContent.Create(request, options: jsonOptions)
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Category>(
                        $"Failed to create category for guild {guildId}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                    .ReadFromJsonAsync<Category>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
            };

    /// <summary>
    /// Updates an existing category for a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to update.</param>
    /// <param name="request">The category update request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated category.</returns>
    public async Task<Result<Category>> UpdateAsync(
        GuildId guildId, int categoryId, CategoryRequests.UpdateCategory request, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Put, $"guilds/{guildId}/categories/{categoryId}")
                {
                    Headers = { Authorization = authenticationHeader },
                    Content = JsonContent.Create(request, options: jsonOptions)
                }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Category>(
                        $"Failed to update category {categoryId} for guild {guildId}: {(int)statusCode} ({reasonPhrase})"),
                var response => await Try(() => response.Content
                    .ReadFromJsonAsync<Category>(jsonOptions, cancellationToken: token)).ConfigureAwait(false)
            };

    /// <summary>
    /// Deletes a category from a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result indicating success (true) or not found (false).</returns>
    public async Task<Result<bool>> DeleteAsync(GuildId guildId, int categoryId, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, $"guilds/{guildId}/categories/{categoryId}")
                {
                    Headers = { Authorization = authenticationHeader }
                }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NoContent } => Success(true),
                { StatusCode: HttpStatusCode.NotFound } => Success(false),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<bool>(
                        $"Failed to delete category {categoryId} for guild {guildId}: {(int)statusCode} ({reasonPhrase})"),
                _ => Success(true)
            };
}