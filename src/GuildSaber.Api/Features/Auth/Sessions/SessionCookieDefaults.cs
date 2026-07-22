namespace GuildSaber.Api.Features.Auth.Sessions;

public static class SessionCookieDefaults
{
    public const string AuthenticationScheme = "SessionCookie";
    public const string ProductionCookieName = "__Host-GuildSaber.Session";
    public const string DevelopmentCookieName = "GuildSaber.Session";
    public const string RequestVerificationHeaderName = "X-GuildSaber-Request";
    public const string RequestVerificationHeaderValue = "1";
}