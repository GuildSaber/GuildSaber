using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Hangfire.Configuration;

public class RetryPolicyOptions
{
    public const string RetryPolicyOptionsSectionsKey = "Hangfire:RetryPolicy";

    [Required]
    public int MaxRetryAttempts { get; init; }
}