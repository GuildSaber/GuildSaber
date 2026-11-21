using System.Net.Mime;
using Amazon.S3;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Setup;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Guilds;

public sealed class GuildStorage(AmazonS3Client s3Client, IOptions<S3Settings> settings)
{
    public Task<UnitResult<string>> UploadLogoAsync(GuildId guildId, Stream jpeg, CancellationToken token)
        => s3Client.UploadAsync(settings.Value.BucketName, $"guilds/{guildId}/logo.jpg", jpeg,
            MediaTypeNames.Image.Jpeg, token);

    public Task<UnitResult<string>> UploadBannerAsync(GuildId guildId, Stream jpeg, CancellationToken token)
        => s3Client.UploadAsync(settings.Value.BucketName, $"guilds/{guildId}/banner.jpg", jpeg,
            MediaTypeNames.Image.Jpeg, token);
}