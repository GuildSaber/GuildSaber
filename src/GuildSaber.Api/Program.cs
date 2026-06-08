using GuildSaber.Api;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members;
using GuildSaber.Api.Features.LegacyGS;
using GuildSaber.Api.Features.Players;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Api.Setup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MyCSharp.HttpUserAgentParser.AspNetCore.DependencyInjection;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using Scalar.AspNetCore;

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

builder.Services.AddCors(options => options
    .AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services
    .AddPlayersFeature()
    .AddScoresFeature()
    .AddGuildsFeature()
    .AddGuildMembersFeature()
    .AddLegacyGuildSaberFeature()
    .AddRankedMapsFeature();

var app = builder.Build();

app.UseExceptionHandler()
    .UseStatusCodePages();

app.UseCors()
    .UseAuthentication()
    .UseAuthorization()
    .UseOutputCache();

// https://github.com/Arcenox-co/TickerQ/issues/788
/*if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
    app.UseTickerQ();*/

app.UseFileServer("/website");

app.MapOpenApi().CacheOutput();
app.MapDefaultEndpoints()
    .MapEndpoints<IApiMarker>();

app.MapScalarApiReference("/docs", options => options
    .WithTitle("GuildSaber's Api")
    .WithTheme(ScalarTheme.Purple)
    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
    .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
    .EnablePersistentAuthentication());

app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

app.MapFallbackToFile("/website/{*path:nonfile}", "website/index.html", new StaticFileOptions
{
    RequestPath = "/website"
});

app.Run();