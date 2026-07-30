namespace GuildSaber.Api.Features.Auth.Sessions;

public sealed class SessionCookieService(IHostEnvironment environment)
{
    public string CookieName => environment.IsDevelopment()
        ? SessionCookieDefaults.DevelopmentCookieName
        : SessionCookieDefaults.ProductionCookieName;

    public void Append(HttpResponse response, string token, DateTimeOffset expiresAt)
    {
        DisableResponseCaching(response);
        response.Cookies.Append(CookieName, token, CreateCookieOptions(response, expiresAt));
    }

    public void Delete(HttpResponse response)
    {
        DisableResponseCaching(response);
        response.Cookies.Delete(CookieName, CreateCookieOptions(response, expiresAt: null));
    }

    private static void DisableResponseCaching(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private CookieOptions CreateCookieOptions(HttpResponse response, DateTimeOffset? expiresAt)
    {
        var secure = !environment.IsDevelopment() || response.HttpContext.Request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        };
    }
}
