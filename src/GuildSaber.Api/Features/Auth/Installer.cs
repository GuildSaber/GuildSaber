using System.Text;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.Auth.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace GuildSaber.Api.Features.Auth;

public static class AuthInstaller
{
    public static WebApplicationBuilder AddAuthFeature(this WebApplicationBuilder builder)
    {
        var authSettings = builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey);

        builder.Services
            .AddSingleton<JwtService>()
            .AddScoped<IClaimsTransformation, GuildPermissionClaimTransformer>()
            .AddScoped<SessionValidator>()
            .AddScoped<AuthService>()
            .AddSingleton<IAuthorizationHandler, GuildPermissionHandler>()
            .AddSingleton<ICustomApiKeyAuthenticationService, CustomApiKeyAuthenticationService>();

        builder.Services.AddAuthentication(options => options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme)
            .AddBeatLeader(options =>
            {
                var settings = authSettings.GetSection(nameof(AuthSettings.BeatLeader)).Get<BeatLeaderAuthSettings>()!;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.SignInScheme = "BeatLeaderCookies";
                options.SaveTokens = true;
            }).AddCookie("BeatLeaderCookies", options =>
            {
                options.Cookie.Name = "BeatLeader";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
            }).AddDiscord(options =>
            {
                var settings = authSettings.GetSection(nameof(AuthSettings.Discord)).Get<DiscordAuthSettings>()!;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.SignInScheme = "DiscordCookies";
                options.SaveTokens = true;
            }).AddCookie("DiscordCookies", options =>
            {
                options.Cookie.Name = "Discord";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
            }).AddJwtBearer(options =>
            {
                var settings = authSettings.GetSection(nameof(AuthSettings.Jwt)).Get<JwtAuthSettings>()!;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sessionValidator =
                            context.HttpContext.RequestServices.GetRequiredService<SessionValidator>();
                        var sessionId = context.Principal?.GetSessionId();
                        if (sessionId is null)
                        {
                            context.Fail("Session ID not found in token.");
                            return;
                        }

                        // Validate session from the database + enrich principal with PlayerId claim.
                        var sessionResult =
                            await sessionValidator.ValidateAndApplySessionAsync(sessionId.Value, context.Principal);
                        if (sessionResult.TryGetError(out var error))
                            context.Fail(error);
                    }
                };
            }).AddScheme<AuthenticationSchemeOptions, CustomApiKeyAuthenticationHandler>(
                BasicAuthenticationDefaults.AuthenticationScheme, _ => { });

        builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    BasicAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddManagerAuthorizationPolicy()
            .AddGuildAuthorizationPolicies();

        return builder;
    }
}