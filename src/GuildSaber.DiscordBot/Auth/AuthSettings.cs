using System.ComponentModel.DataAnnotations;

namespace GuildSaber.DiscordBot.Auth;

public class AuthSettings
{
    public const string AuthSettingsSectionKey = "AuthSettings";

    [Required] public required string ApiKey { get; init; }
}