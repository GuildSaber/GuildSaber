using System.Net.Mime;
using Amazon.S3;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Setup;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Guilds.Categories;

public sealed class CategoryStorage(AmazonS3Client s3Client, IOptions<S3Settings> settings)
{
    public Task<UnitResult<string>> UploadLogoAsync(CategoryId categoryId, Stream png, CancellationToken token)
        => s3Client.UploadAsync(settings.Value.BucketName, $"categories/{categoryId}/logo.png", png,
            MediaTypeNames.Image.Png, token);
}