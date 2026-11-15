using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.Guilds.Achievements.Http;
using GuildSaber.CSharpClient.Models;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.CSharpClient.Routes.Guilds.Achievements;

/// <summary>
/// Client for interacting with achievement endpoints.
/// </summary>
public sealed class AchievementClient(
    HttpClient httpClient,
    Uri cdnBaseUri,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    /// <summary>
    /// Gets an achievement by its ID.
    /// </summary>
    /// <param name="achievementId">The ID of the achievement to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the achievement if found, or null if not found.</returns>
    public async Task<Result<Achievement?>> GetByIdAsync(
        int achievementId, CancellationToken cancellationToken = default)
        => await httpClient.GetAsync($"achievements/{achievementId}", cancellationToken)
                .ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Achievement?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Achievement?>(
                        $"Failed to retrieve achievement with ID {achievementId}, status code: {(int)statusCode} ({reasonPhrase})"),
                var response => await ReadAchievementAsync(response.Content, cancellationToken)
                    .ConfigureAwait(false)
            };

    /// <summary>
    /// Gets all achievements for a context, optionally filtered by category.
    /// </summary>
    /// <param name="contextId">The context identifier.</param>
    /// <param name="categoryId">Optional category ID to filter by.</param>
    /// <param name="hasCategory">If false, returns only achievements with no category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing an array of achievements.</returns>
    public async Task<Result<Achievement[]>> GetByContextIdAsync(
        int contextId, int? categoryId, bool hasCategory, CancellationToken cancellationToken = default)
    {
        var url = $"contexts/{contextId}/achievements";
        if (categoryId.HasValue) url += $"?categoryId={categoryId.Value}&hasCategory={hasCategory}";
        else url += $"?hasCategory={hasCategory}";

        return await httpClient.GetAsync(url, cancellationToken)
                .ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Achievement[]>(
                        $"Failed to retrieve achievements for context {contextId}: {(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                            .ReadFromJsonAsync<Achievement[]>(jsonOptions, cancellationToken: cancellationToken),
                        cancellationToken)
                    .ConfigureAwait(false))!
            };
    }

    /// <summary>Creates an achievement and queues a member-stat refresh.</summary>
    /// <param name="contextId">Context in which to create the achievement.</param>
    /// <param name="request">Complete achievement fields and completion rule.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The created achievement, or null if the context no longer exists.</returns>
    public Task<Result<Achievement?>> CreateAsync(
        ContextId contextId, AchievementRequests.Definition request, CancellationToken token = default)
        => SaveAsync(HttpMethod.Post, $"contexts/{contextId}/achievements", request, token);

    /// <summary>Updates an achievement. Rule and progression changes queue a member-stat refresh.</summary>
    /// <param name="contextId">Context containing the achievement.</param>
    /// <param name="achievementId">Achievement to update.</param>
    /// <param name="request">Updated fields and completion rule.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The updated achievement, or null if it no longer exists in the context.</returns>
    public Task<Result<Achievement?>> UpdateAsync(
        ContextId contextId, int achievementId, AchievementRequests.Definition request,
        CancellationToken token = default)
        => SaveAsync(HttpMethod.Put, $"contexts/{contextId}/achievements/{achievementId}", request, token);

    /// <summary>Deletes an achievement and queues a refresh of the remaining level progression.</summary>
    /// <param name="contextId">Context containing the achievement.</param>
    /// <param name="achievementId">Achievement to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>True if the achievement was deleted, or false if it no longer exists in the context.</returns>
    public async Task<Result<bool>> DeleteAsync(
        ContextId contextId, int achievementId, CancellationToken token = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete,
            $"contexts/{contextId}/achievements/{achievementId}");
        message.Headers.Authorization = authenticationHeader;
        using var response = await httpClient.SendAsync(message, token).ConfigureAwait(false);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => Success(true),
            HttpStatusCode.NotFound => Success(false),
            _ => Failure<bool>(
                $"Failed to delete achievement {achievementId}: {(int)response.StatusCode} ({response.ReasonPhrase})")
        };
    }

    private async Task<Result<Achievement?>> SaveAsync(
        HttpMethod method, string path, AchievementRequests.Definition request, CancellationToken token)
    {
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = authenticationHeader;
        message.Content = JsonContent.Create(request, options: jsonOptions);
        using var response = await httpClient.SendAsync(message, token).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return Success<Achievement?>(null);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(jsonOptions, token)
                .ConfigureAwait(false);
            return Failure<Achievement?>(problem?.GetMessage() ?? "The achievement definition is invalid.");
        }

        return response.IsSuccessStatusCode
            ? await ReadAchievementAsync(response.Content, token).ConfigureAwait(false)
            : Failure<Achievement?>(
                $"Failed to save achievement: {(int)response.StatusCode} ({response.ReasonPhrase})");
    }

    private Task<Result<Achievement?>> ReadAchievementAsync(HttpContent content, CancellationToken token)
        => TryAsync(() => content.ReadFromJsonAsync<Achievement?>(jsonOptions, token), token);

    public Uri GetCoverUrl(int achievementId) => new(cdnBaseUri, $"achievements/{achievementId}/cover.png");
}