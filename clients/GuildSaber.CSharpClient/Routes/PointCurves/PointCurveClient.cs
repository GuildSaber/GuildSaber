using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Features.PointCurves.Http;
using static GuildSaber.Api.Features.PointCurves.Http.PointCurveResponses;

namespace GuildSaber.CSharpClient.Routes.PointCurves;

/// <summary>
/// Configures percentage accuracy bonuses and edits normalized map curves when their policy permits it.
/// </summary>
public sealed class PointCurveClient(
    HttpClient httpClient,
    AuthenticationHeaderValue? authenticationHeader,
    JsonSerializerOptions jsonOptions)
{
    private static string SettingsUrl(GuildId guildId, int pointId)
        => $"guilds/{guildId}/points/{pointId}/curve-settings";

    private static string CurveUrl(ContextId contextId, RankedMapId rankedMapId, int pointId)
        => $"contexts/{contextId}/ranked-maps/{rankedMapId}/points/{pointId}/curve";

    /// <summary>Gets shared curves, the bonus ratio and map-curve policy. Returns null if the point was not found.</summary>
    public async Task<Result<Settings?>> GetSettingsAsync(
        GuildId guildId, int pointId, CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, SettingsUrl(guildId, pointId))
            {
                Headers = { Authorization = authenticationHeader }
            }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Settings?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Settings?>(
                        $"Failed to retrieve curve settings for point {pointId} in guild {guildId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Settings?>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false)
            };

    /// <summary>Replaces the point's shared curve settings and schedules affected scores for refresh.</summary>
    public async Task<Result<Settings>> UpdateSettingsAsync(
        GuildId guildId, int pointId, PointCurveRequests.UpdateSettings request, CancellationToken token = default)
        => await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Put, SettingsUrl(guildId, pointId))
            {
                Headers = { Authorization = authenticationHeader },
                Content = JsonContent.Create(request, options: jsonOptions)
            }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Settings>(
                        $"Failed to update curve settings for point {pointId} in guild {guildId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Settings>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };

    /// <summary>Gets the effective normalized shape, including shared fallback.</summary>
    public async Task<Result<Curve?>> GetCurveAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, CurveUrl(contextId, rankedMapId, pointId))
                {
                    Headers = { Authorization = authenticationHeader }
                }, token).ConfigureAwait(false) switch
            {
                { StatusCode: HttpStatusCode.NotFound } => Success<Curve?>(null),
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Curve?>(
                        $"Failed to retrieve curve for map {rankedMapId}, point {pointId}, context {contextId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Curve?>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false)
            };

    /// <summary>Publishes normalized accuracy/bonus-fraction samples for an editable map curve.</summary>
    public async Task<Result<Curve>> PutCurveAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId,
        PointCurveRequests.PutCurve request, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Put, CurveUrl(contextId, rankedMapId, pointId))
                {
                    Headers = { Authorization = authenticationHeader },
                    Content = JsonContent.Create(request, options: jsonOptions)
                }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Curve>(
                        $"Failed to publish curve for map {rankedMapId}, point {pointId}, context {contextId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Curve>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };

    /// <summary>Removes an editable override and schedules scores to use the shared fallback.</summary>
    public async Task<Result<Curve>> DeleteCurveAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, CurveUrl(contextId, rankedMapId, pointId))
                {
                    Headers = { Authorization = authenticationHeader }
                }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Curve>(
                        $"Failed to delete curve for map {rankedMapId}, point {pointId}, context {contextId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Curve>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };

    /// <summary>Previews a shape and resulting base-plus-bonus rewards without publishing.</summary>
    public async Task<Result<Preview>> PreviewAsync(
        ContextId contextId, RankedMapId rankedMapId, int pointId,
        PointCurveRequests.Preview request, CancellationToken token = default)
        => await httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, $"{CurveUrl(contextId, rankedMapId, pointId)}/preview")
                {
                    Headers = { Authorization = authenticationHeader },
                    Content = JsonContent.Create(request, options: jsonOptions)
                }, token).ConfigureAwait(false) switch
            {
                { IsSuccessStatusCode: false, StatusCode: var statusCode, ReasonPhrase: var reasonPhrase }
                    => Failure<Preview>(
                        $"Failed to preview curve for map {rankedMapId}, point {pointId}, context {contextId}: " +
                        $"{(int)statusCode} ({reasonPhrase})"),
                var response => (await TryAsync(() => response.Content
                        .ReadFromJsonAsync<Preview>(jsonOptions, cancellationToken: token), token)
                    .ConfigureAwait(false))!
            };
}