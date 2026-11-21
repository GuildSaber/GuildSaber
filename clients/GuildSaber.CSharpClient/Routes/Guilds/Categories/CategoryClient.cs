using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Categories.Http;
using GuildSaber.Api.Shared;
using GuildSaber.CSharpClient.Models;
using static GuildSaber.Api.Features.Guilds.Categories.Http.CategoryResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Categories;

/// <summary>
/// Client for interacting with category endpoints.
/// </summary>
public sealed class CategoryClient(
    HttpClient httpClient,
    Uri cdnBaseUri,
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
            var response => await TryAsync(() => response.Content
                    .ReadFromJsonAsync<PagedList<Category>>(jsonOptions, cancellationToken: token), token)
                .ConfigureAwait(false)
        };

    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    /// <param name="categoryId">The ID of the category to retrieve.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the category if found, or null if not found.</returns>
    public async Task<Result<Category?>> GetByIdAsync(CategoryId categoryId, CancellationToken token = default)
        => await httpClient.GetAsync($"categories/{categoryId}", token).ConfigureAwait(false) switch
        {
            { StatusCode: HttpStatusCode.NotFound } => Success<Category?>(null),
            { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                => Failure<Category?>(
                    $"Failed to retrieve category with ID {categoryId}: {(int)statusCode} ({reasonPhrase})"),
            var response => await TryAsync(() => response.Content
                .ReadFromJsonAsync<Category?>(jsonOptions, cancellationToken: token), token).ConfigureAwait(false)
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
            var response => (await TryAsync(() => response.Content
                .ReadFromJsonAsync<Category[]>(jsonOptions, cancellationToken: token), token).ConfigureAwait(false))!
        };

    /// <summary>
    /// Creates a new category for a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild to create the category for.</param>
    /// <param name="request">The category creation request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the created category.</returns>
    public Task<Result<Category>> CreateAsync(
        GuildId guildId, CategoryRequests.CreateCategory request, CancellationToken token = default)
        => SaveAsync(HttpMethod.Post, $"guilds/{guildId}/categories", request, token);

    /// <summary>
    /// Updates an existing category for a guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to update.</param>
    /// <param name="request">The category update request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result containing the updated category.</returns>
    public Task<Result<Category>> UpdateAsync(
        GuildId guildId, CategoryId categoryId, CategoryRequests.UpdateCategory request,
        CancellationToken token = default)
        => SaveAsync(HttpMethod.Put, $"guilds/{guildId}/categories/{categoryId}", request, token);

    private async Task<Result<Category>> SaveAsync<T>(
        HttpMethod method, string path, T request, CancellationToken token)
    {
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = authenticationHeader;
        message.Content = JsonContent.Create(request, options: jsonOptions);
        using var response = await httpClient.SendAsync(message, token).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Failure<Category>("This category or its guild no longer exists.");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(jsonOptions, token)
                .ConfigureAwait(false);
            return Failure<Category>(problem?.GetMessage() ?? "The category name or description is invalid.");
        }

        return response.IsSuccessStatusCode
            ? await ReadCategoryAsync(response.Content, token).ConfigureAwait(false)
            : Failure<Category>($"Failed to save category: {(int)response.StatusCode} ({response.ReasonPhrase})");
    }

    private Task<Result<Category>> ReadCategoryAsync(HttpContent content, CancellationToken token)
        => TryAsync(() => content.ReadFromJsonAsync<Category>(jsonOptions, token), token);

    /// <summary>
    /// Deletes a category from a guild.
    /// </summary>
    /// <remarks>
    /// Also deletes its achievements across the guild's contexts and removes category associations and member stats.
    /// Proposal history retains its category and achievement snapshots.
    /// </remarks>
    /// <param name="guildId">The ID of the guild the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result indicating success (true) or not found (false).</returns>
    public async Task<Result<bool>> DeleteAsync(
        GuildId guildId, CategoryId categoryId, CancellationToken token = default)
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

    /// <summary>
    /// Replaces the logo of a category with a square static PNG image up to 2 MiB and 1024 pixels per side.
    /// </summary>
    /// <param name="guildId">The ID of the guild the category belongs to.</param>
    /// <param name="categoryId">The ID of the category to update.</param>
    /// <param name="png">The PNG image stream. The request disposes the stream after sending it.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A result indicating whether the logo was uploaded.</returns>
    public Task<Result> UploadLogoAsync(
        GuildId guildId, CategoryId categoryId, Stream png, CancellationToken token = default)
        => httpClient.UploadImageAsync($"guilds/{guildId}/categories/{categoryId}/logo", png, "logo.png", "image/png",
            authenticationHeader, jsonOptions, "category logo",
            $"Category {categoryId} not found in guild {guildId}.", token);

    public Uri GetLogoUrl(int id) => new(cdnBaseUri, $"categories/{id}/logo.png");
}
