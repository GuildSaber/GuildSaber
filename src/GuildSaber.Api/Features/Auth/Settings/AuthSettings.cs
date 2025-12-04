using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Features.Auth.Settings;

public class AuthSettings
{
    public const string AuthSettingsSectionKey = "AuthSettings";

    [Required] public required SessionSettings Session { get; init; }
    [Required] public required JwtAuthSettings Jwt { get; init; }
    [Required] public required BeatLeaderAuthSettings BeatLeader { get; init; }
    [Required] public required DiscordAuthSettings Discord { get; init; }
    [Required] public required RedirectSettings Redirect { get; init; }
    [Required] public required ManagerSettings Manager { get; init; }
    [Required] public required ApiKeyAuthSettings ApiKey { get; init; }
}

public class ManagerSettings
{
    [Required] public required string[] SteamIds { get; init; }
}

public class RedirectSettings
{
    public string[] AllowedOriginUrls { get; init; } = [];
}

public class SessionSettings
{
    [Required] public required TimeSpan ExpireAfter { get; init; }
    [Required] public required int MaxSessionCount { get; init; }
}

public class JwtAuthSettings
{
    [Required] public required string Issuer { get; init; }
    [Required] public required string Audience { get; init; }
    [Required] public required string Secret { get; init; }
}

public class BeatLeaderAuthSettings
{
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }
}

public class DiscordAuthSettings
{
    [Required] public required string ClientId { get; init; }
    [Required] public required string ClientSecret { get; init; }
}

public class ApiKeyAuthSettings
{
    [Required] public required string Key { get; init; }
}