namespace GuildSaber.Api.Features.Auth.Sessions;

public sealed class SessionCookieService(IHostEnvironment environment)
{
    public string CookieName => environment.IsDevelopment()
        ? SessionCookieDefaults.DevelopmentCookieName
        : SessionCookieDefaults.ProductionCookieName;

    public void Append(HttpResponse response, string token, DateTimeOffset expiresAt)
    {
        DisableResponseCaching(response);
        response.Cookies.Append(CookieName, token, CreateCookieOptions(expiresAt));
    }

    public void Delete(HttpResponse response)
    {
        DisableResponseCaching(response);
        response.Cookies.Delete(CookieName, CreateCookieOptions(expiresAt: null));
    }

    private static void DisableResponseCaching(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private CookieOptions CreateCookieOptions(DateTimeOffset? expiresAt)
        => new()
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        };
}