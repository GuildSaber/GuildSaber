using System.Reflection;
using GuildSaber.Api;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Api.Features.LegacyGS;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Api.Setup;
using MyCSharp.HttpUserAgentParser.AspNetCore.DependencyInjection;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using Scalar.AspNetCore;
using TickerQ.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services
    .AddHybridCache();

builder
    .AddSettings()
    .AddAuthFeature()
    .AddDatabase()
    .AddTickerQ()
    .AddExternalApis()
    .AddBackgroundQueues()
    .AddApiDefaults();

builder.Services
    .AddHttpContextAccessor()
    .AddHttpUserAgentParser()
    .AddHttpUserAgentParserAccessor();

var allowedWebsiteOrigins = builder.Configuration
    .GetSection($"{AuthSettings.AuthSettingsSectionKey}:{nameof(AuthSettings.Redirect)}")
    .Get<RedirectSettings>()?.AllowedOriginUrls
    .Select(url => new Uri(url).GetLeftPart(UriPartial.Authority))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

builder.Services.AddCors(options => options
    .AddDefaultPolicy(policy => policy
        .WithOrigins(allowedWebsiteOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

builder.Services
    .AddPlayersFeature()
    .AddScoresFeature()
    .AddGuildsFeature()
    .AddGuildMembersFeature()
    .AddLegacyGuildSaberFeature()
    .AddRankedScoresFeature()
    .AddRankedMapsFeature();

var app = builder.Build();

app.UseExceptionHandler()
    .UseStatusCodePages();

app.UseCors()
    .UseAuthentication()
    .UseMiddleware<SessionCookieRequestVerificationMiddleware>()
    .UseAuthorization()
    .UseOutputCache();

// https://github.com/Arcenox-co/TickerQ/issues/788
if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
    app.UseTickerQ();

app.UseFileServer("/website");

app.MapOpenApi().CacheOutput();
app.MapDefaultEndpoints()
    .MapEndpoints<IApiMarker>();

app.MapScalarApiReference("/docs", options => options
    .WithTitle("GuildSaber's Api")
    .WithTheme(ScalarTheme.Purple)
    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
    .AddPreferredSecuritySchemes(
        SessionCookieDefaults.AuthenticationScheme,
        SessionCookieDefaults.RequestVerificationHeaderName)
    .AddApiKeyAuthentication(SessionCookieDefaults.RequestVerificationHeaderName, x =>
    {
        x.Name = SessionCookieDefaults.RequestVerificationHeaderName;
        x.Value = SessionCookieDefaults.RequestVerificationHeaderValue;
    })
    .EnablePersistentAuthentication());

app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

app.MapFallbackToFile("/website/{*path:nonfile}", "website/index.html", new StaticFileOptions
{
    RequestPath = "/website"
});

app.Run();
