using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Features.Auth;

public class AuthSettings
{
    public const string AuthSettingsSectionKey = "AuthSettings";

    [Required] public required SessionSettings Session { get; init; }
    [Required] public required SessionCookieAuthSettings SessionCookie { get; init; }
    [Required] public required BeatLeaderAuthSettings BeatLeader { get; init; }
    [Required] public required DiscordAuthSettings Discord { get; init; }
    [Required] public required RedirectSettings Redirect { get; init; }
    [Required] public required ManagerSettings Manager { get; init; }
    [Required] public required ApiKeyAuthSettings ApiKey { get; init; }
    [Required] public required TickerQAuthSettings TickerQ { get; init; }
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

public class SessionCookieAuthSettings
{
    [Required] public required string Issuer { get; init; }
    [Required] public required string Audience { get; init; }
    [Required, MinLength(32)] public required string SigningKey { get; init; }
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

public class TickerQAuthSettings
{
    [Required] public required string ApiKey { get; init; }
}