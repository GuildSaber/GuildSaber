using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace GuildSaber.Api.Features.Auth;

public static class AuthInstaller
{
    public static WebApplicationBuilder AddAuthFeature(this WebApplicationBuilder builder)
    {
        var authSettings = builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey);

        builder.Services
            .AddSingleton<SessionTokenService>()
            .AddSingleton<SessionCookieService>()
            .AddScoped<IClaimsTransformation, GuildPermissionClaimTransformer>()
            .AddScoped<SessionValidator>()
            .AddScoped<AuthService>()
            .AddSingleton<IAuthorizationHandler, GuildPermissionHandler>()
            .AddSingleton<ICustomApiKeyAuthenticationService, CustomApiKeyAuthenticationService>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = SessionCookieDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = SessionCookieDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(
                SessionCookieDefaults.AuthenticationScheme, _ => { })
            .AddBeatLeader(options =>
            {
                var settings = authSettings.GetSection(nameof(AuthSettings.BeatLeader)).Get<BeatLeaderAuthSettings>()!;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.SignInScheme = AuthConstants.BeatLeaderCookieScheme;
                options.SaveTokens = false;
            }).AddCookie(AuthConstants.BeatLeaderCookieScheme, options =>
            {
                options.Cookie.Name = "BeatLeader";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            }).AddDiscord(options =>
            {
                var settings = authSettings.GetSection(nameof(AuthSettings.Discord)).Get<DiscordAuthSettings>()!;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.SignInScheme = AuthConstants.DiscordCookieScheme;
                options.SaveTokens = false;
            }).AddCookie(AuthConstants.DiscordCookieScheme, options =>
            {
                options.Cookie.Name = "Discord";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            }).AddScheme<AuthenticationSchemeOptions, CustomApiKeyAuthenticationHandler>(
                BasicAuthenticationDefaults.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(
                    SessionCookieDefaults.AuthenticationScheme,
                    BasicAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(AuthConstants.SessionPolicy, policy => policy
                .AddAuthenticationSchemes(SessionCookieDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddManagerAuthorizationPolicy()
            .AddGuildAuthorizationPolicies();

        return builder;
    }
}