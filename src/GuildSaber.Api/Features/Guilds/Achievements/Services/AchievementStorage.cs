using System.Net.Mime;
using Amazon.S3;
using CSharpFunctionalExtensions;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Setup;
using Microsoft.Extensions.Options;

namespace GuildSaber.Api.Features.Guilds.Achievements.Services;

public sealed class AchievementStorage(AmazonS3Client s3Client, IOptions<S3Settings> settings)
{
    public Task<UnitResult<string>> UploadCoverAsync(int achievementId, Stream png, CancellationToken token)
        => s3Client.UploadAsync(settings.Value.BucketName, $"achievements/{achievementId}/cover.png", png,
            MediaTypeNames.Image.Png, token);
}