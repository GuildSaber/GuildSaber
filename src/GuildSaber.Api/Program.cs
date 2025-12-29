using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.CustomApiKey;
using GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.OldGuildSaber;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyCSharp.HttpUserAgentParser.AspNetCore.DependencyInjection;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

#region Configuration & Options

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
    .AddOptionsWithValidateOnStart<JwtAuthSettings>()
    .Bind(authSettings.GetSection(nameof(AuthSettings.Jwt))).ValidateDataAnnotations();
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

var guildSettings = builder.Configuration.GetSection(GuildSettings.GuildSettingsSectionKey);
builder.Services
    .AddOptionsWithValidateOnStart<GuildSettings>()
    .Bind(guildSettings).ValidateDataAnnotations();
builder.Services
    .AddOptionsWithValidateOnStart<GuildCreationSettings>()
    .Bind(guildSettings.GetSection(nameof(GuildSettings.Creation))).ValidateDataAnnotations();
builder.Services
    .AddOptionsWithValidateOnStart<RankedMapSettings>()
    .Bind(builder.Configuration.GetSection(RankedMapSettings.RankedMapSettingsSectionKey)).ValidateDataAnnotations();

#endregion

#region Core Services

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpUserAgentParser()
    .AddHttpUserAgentParserAccessor();

#endregion

#region Database

builder.AddNpgsqlDbContext<ServerDbContext>(connectionName: Constants.ServerDbConnectionStringKey,
    configureDbContextOptions: options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

#endregion

#region Authentication & Authorization

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
        options.Cookie.SameSite = SameSiteMode.None;
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
        }
    ).AddCookie("DiscordCookies", options =>
    {
        options.Cookie.Name = "Discord";
        options.Cookie.SameSite = SameSiteMode.None;
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
                var sessionValidator = context.HttpContext.RequestServices.GetRequiredService<SessionValidator>();
                var sessionId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (sessionId is null)
                {
                    context.Fail("Session ID not found in token.");
                    return;
                }

                var parseResult = UuidV7.TryParse(sessionId);
                if (!parseResult.TryGetValue(out var sessionUuId))
                {
                    context.Fail($"Invalid session ID format: {parseResult.Error}");
                    return;
                }

                // Validate session from the database + enrich principal with PlayerId claim.
                var sessionResult = await sessionValidator.ValidateSessionAsync(sessionUuId, context.Principal);
                if (sessionResult.TryGetError(out var error))
                    context.Fail(error);
            }
        };
    }).AddScheme<AuthenticationSchemeOptions, CustomApiKeyAuthenticationHandler>(
        BasicAuthenticationDefaults.AuthenticationScheme, _ => { }
    );

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            BasicAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build())
    .AddManagerAuthorizationPolicy()
    .AddGuildAuthorizationPolicies();

builder.Services.AddHybridCache();

#endregion

#region External Services & HTTP Clients

// Configured pooled connection lifetime to avoid DNS issues in long-running services.
#pragma warning disable EXTEXP0001
builder.Services.AddHttpClient<BeatLeaderApi>(client =>
    {
        client.BaseAddress = new Uri("https+http://beatleader-api");
        client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
    }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
    // Remove the resilience handlers because some the timeout policies interfere with endpoints returning 500s on purpose.
    .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

builder.Services.AddHttpClient<ScoreSaberApi>(client =>
    {
        client.BaseAddress = new Uri("https+http://scoresaber-api");
        client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
    }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

builder.Services.AddHttpClient<BeatSaverApi>(client =>
    {
        client.BaseAddress = new Uri("https+http://beatsaver-api");
        client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
    }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

builder.Services
    .AddHttpClient<OldGuildSaberApi>(client => { client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber"); })
    .UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

// Since they hold state, they should be transient.
builder.Services.AddTransient<BeatLeaderGeneralSocketStream>(_ =>
{
    var uri = builder.Configuration.GetValue<Uri>("services:beatleader-socket:default:0");
    ArgumentNullException.ThrowIfNull(uri, "BeatLeader socket URI is not configured in service discovery.");
    return new BeatLeaderGeneralSocketStream(uri);
});

#endregion

#region Background Services

builder.Services.AddTransient<ScoreAddOrUpdatePipeline>();
builder.Services.AddTransient<PlayerScoresPipeline>();
builder.Services.AddTransient<MemberPointStatsPipeline>();
builder.Services.AddTransient<MemberLevelStatsPipeline>();
//builder.Services.AddHostedService<BLScoreSyncWorker>();
builder.Services.AddHostedService<QueueProcessingService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));

#endregion

#region API Configuration

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

OpenApiTypeTransformer.MapType<GuildId>(new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" });
OpenApiTypeTransformer.MapType<ContextId>(new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" });
OpenApiTypeTransformer.MapType<BeatSaverKey>(new OpenApiSchema { Type = JsonSchemaType.String, Example = "a3c3" });
OpenApiTypeTransformer.MapType<SongHash>(new OpenApiSchema
    { Type = JsonSchemaType.String, Example = "ABCD1234EFGH5678IJKL9012MNOP3456QRST7890" });
OpenApiTypeTransformer.MapType<SSLeaderboardId>(new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" });
OpenApiTypeTransformer.MapType<BLLeaderboardId>(new OpenApiSchema { Type = JsonSchemaType.String, Example = "a3c391" });
OpenApiTypeTransformer.MapType<RankedMapRequest.EModifiers>(new OpenApiSchema
    { Example = nameof(RankedMapRequest.EModifiers.None) });
builder.Services.AddOpenApi(options =>
{
    options.AddGlobalProblemDetails()
        .AddBearerSecurityScheme()
        .AddEndpointsHttpSecuritySchemeResolution()
        .AddTagDescriptionSupport()
        .AddScalarTransformers()
        .AddTypeTransformationSupport();
});

builder.Services.AddValidation();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    if (context.Exception is BadHttpRequestException badHttpRequestException)
    {
        context.ProblemDetails.Title = "Bad Request";
        context.ProblemDetails.Detail = badHttpRequestException.Message;
        context.ProblemDetails.Status = StatusCodes.Status400BadRequest;

        context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    }

    context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
    context.ProblemDetails.Extensions
        .TryAdd("traceId", context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity.Id);
});
builder.Services.AddEndpoints<Program>(builder.Configuration);

#endregion

var app = builder.Build();

#region Middleware Pipeline

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Endpoints & UI

app.MapOpenApi();
app.MapDefaultEndpoints()
    .MapEndpoints<Program>();

app.MapScalarApiReference("/", options => options
    .WithTitle("GuildSaber's Api")
    .WithTheme(ScalarTheme.Purple)
    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
    .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
    .EnablePersistentAuthentication()
);

#endregion

app.Run();