using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Common.Settings;

namespace GuildSaber.Api.Setup;

public static class SettingsSetup
{
    public static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        var authSettings = builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey);
        builder.Services
            .AddOptionsWithValidateOnStart<AuthSettings>()
            .Bind(authSettings).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<ManagerSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.Manager))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<SessionSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.Session))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<SessionCookieAuthSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.SessionCookie))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<BeatLeaderAuthSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.BeatLeader))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<DiscordAuthSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.Discord))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<ApiKeyAuthSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.ApiKey))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<RedirectSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.Redirect))).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<TickerQAuthSettings>()
            .Bind(authSettings.GetSection(nameof(AuthSettings.TickerQ))).ValidateDataAnnotations();

        var guildSettings = builder.Configuration.GetSection(GuildSettings.GuildSettingsSectionKey);
        builder.Services
            .AddOptionsWithValidateOnStart<GuildSettings>()
            .Bind(guildSettings).ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<GuildCreationSettings>()
            .Bind(guildSettings.GetSection(nameof(GuildSettings.Creation))).ValidateDataAnnotations();

        builder.Services
            .AddOptionsWithValidateOnStart<RankedMapSettings>()
            .Bind(builder.Configuration.GetSection(RankedMapSettings.RankedMapSettingsSectionKey))
            .ValidateDataAnnotations();
        builder.Services
            .AddOptionsWithValidateOnStart<LinkSettings>()
            .Bind(builder.Configuration.GetSection(LinkSettings.LinkSettingsSectionsKey))
            .ValidateDataAnnotations();

        return builder;
    }
}