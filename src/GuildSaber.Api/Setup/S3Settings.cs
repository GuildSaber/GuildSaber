using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Setup;

public class S3Settings
{
    public const string SectionKey = "S3Settings";

    [Required] public required Uri Endpoint { get; init; }
    [Required] public required string BucketName { get; init; }
    [Required] public required string AccessKeyId { get; init; }
    [Required] public required string SecretAccessKey { get; init; }
}