using System.ComponentModel.DataAnnotations;

namespace GuildSaber.DiscordBot.Settings;

public class AuthSettings
{
    public const string AuthSettingsSectionKey = "AuthSettings";

    [Required] public required string ApiKey { get; init; }
}