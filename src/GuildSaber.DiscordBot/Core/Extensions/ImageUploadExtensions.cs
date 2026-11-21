using CSharpFunctionalExtensions;
using Discord;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Helpers;
using Polly;

namespace GuildSaber.DiscordBot.Core.Extensions;

internal static class ImageUploadExtensions
{
    extension(IDiscordInteraction interaction)
    {
        public async Task<bool> ReportImageUploadAsync(Task<Result> upload, string confirmation)
        {
            var result = await (interaction.HasResponded ? Task.CompletedTask : interaction.DeferAsync(),
                upload).WhenAll();
            await interaction.FollowupAsync(result.IsSuccess ? confirmation : result.Error,
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return result.IsSuccess;
        }
    }

    extension(IHttpClientFactory httpClientFactory)
    {
        public async Task<Result> UploadImageAsync<TState>(
            IAttachment image, TState state, Func<TState, Stream, Task<Result>> upload)
        {
            if (image.Size is <= 0 or > ImageUploadLimits.MaxBytes)
                return Failure("Choose an image up to 2 MiB.");

            using var httpClient = httpClientFactory.CreateClient();
            // Buffer the bounded attachment so the API client can retry the upload.
            httpClient.MaxResponseContentBufferSize = ImageUploadLimits.MaxBytes;
            try
            {
                using var response = await httpClient.GetAsync(image.Url);
                return response.IsSuccessStatusCode
                    ? await upload(state, await response.Content.ReadAsStreamAsync())
                    : Failure("Could not download the attachment from Discord. Try uploading it again.");
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
                                                                                or ExecutionRejectedException)
            {
                return Failure("Could not transfer the image. Try uploading it again.");
            }
        }
    }
}
