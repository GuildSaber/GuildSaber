using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using GuildSaber.CSharpClient.Models;

namespace GuildSaber.CSharpClient.Routes.Guilds;

internal static class ImageUploadClient
{
    public static async Task<Result> UploadImageAsync(
        this HttpClient httpClient, string path, Stream image, string fileName, string contentType,
        AuthenticationHeaderValue? authenticationHeader, JsonSerializerOptions jsonOptions,
        string assetName, string notFoundMessage, CancellationToken token)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, path);
        message.Headers.Authorization = authenticationHeader;

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(image) { Headers = { ContentType = new(contentType) } }, "file", fileName);
        message.Content = content;

        using var response = await httpClient.SendAsync(message, token).ConfigureAwait(false);
        return response switch
        {
            { IsSuccessStatusCode: true } => Success(),
            { StatusCode: HttpStatusCode.NotFound } => Failure(notFoundMessage),
            { Content: { Headers.ContentType.MediaType: "application/problem+json" } responseContent }
                when (await responseContent.ReadFromJsonAsync<ProblemDetails>(jsonOptions, token)
                    .ConfigureAwait(false))?.GetMessage() is { } error => Failure(error),
            { StatusCode: var statusCode, ReasonPhrase: var reasonPhrase } => Failure(
                $"Failed to upload {assetName}: {(int)statusCode} ({reasonPhrase})")
        };
    }
}