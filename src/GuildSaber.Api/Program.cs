using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Authorization;
using GuildSaber.Api.Features.Auth.Sessions;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Api.Hangfire;
using GuildSaber.Api.Hangfire.Configuration;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.StrongTypes;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using MyCSharp.HttpUserAgentParser.AspNetCore.DependencyInjection;
using MyCSharp.HttpUserAgentParser.DependencyInjection;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptionsWithValidateOnStart<RetryPolicyOptions>()
    .Bind(builder.Configuration.GetSection(RetryPolicyOptions.RetryPolicyOptionsSectionsKey)).ValidateDataAnnotations();

var authSettings = builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey);
builder.Services
    .AddOptionsWithValidateOnStart<AuthSettings>()
    .Bind(authSettings).ValidateDataAnnotations();
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
    .AddOptionsWithValidateOnStart<RedirectSettings>()
    .Bind(authSettings.GetSection(nameof(AuthSettings.Redirect))).ValidateDataAnnotations();

var guildSettings = builder.Configuration.GetSection(GuildSettings.GuildSettingsSectionKey);
builder.Services
    .AddOptionsWithValidateOnStart<GuildSettings>()
    .Bind(guildSettings).ValidateDataAnnotations();
builder.Services
    .AddOptionsWithValidateOnStart<GuildCreationSettings>()
    .Bind(guildSettings.GetSection(nameof(GuildSettings.Creation))).ValidateDataAnnotations();

var connectionMultiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("cache")!);

builder.AddServiceDefaults();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpUserAgentParser()
    .AddHttpUserAgentParserAccessor();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimTransformer>();
builder.Services.AddScoped<SessionValidator>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IAuthorizationHandler, GuildPermissionHandler>();
builder.Services.AddHttpClient<BeatLeaderApi>(client =>
{
    client.BaseAddress = new Uri("https+http://beatleader-api");
    client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
});
builder.Services.AddTransient<BeatLeaderGeneralSocketStream>(_ =>
{
    var uri = builder.Configuration.GetValue<Uri>("services:beatleader-socket:default:0");
    ArgumentNullException.ThrowIfNull(uri, "BeatLeader socket URI is not configured in service discovery.");
    return new BeatLeaderGeneralSocketStream(uri);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});

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

                var sessionResult = await sessionValidator.ValidateSessionAsync(sessionUuId, context.Principal);
                if (sessionResult.TryGetError(out var error))
                    context.Fail(error);
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddManagerAuthorizationPolicy()
    .AddGuildAuthorizationPolicies();

builder.Services.AddHangfire((serviceCollection, option) =>
{
    option.UseSimpleAssemblyNameTypeSerializer();
    option.UseRecommendedSerializerSettings();
    option.UseRedisStorage(connectionMultiplexer);
    option.UseActivator(new HangfireActivator(serviceCollection));
    option.UseFilter(new AutomaticRetryAttribute
    {
        Attempts = builder.Configuration.GetSection(RetryPolicyOptions.RetryPolicyOptionsSectionsKey)
            .Get<RetryPolicyOptions>()!.MaxRetryAttempts
    });
}).AddHangfireServer();

builder.AddMySqlDbContext<ServerDbContext>(connectionName: Constants.ServerDbConnectionStringKey);
builder.Services.AddOpenApi(options => options
    .AddGlobalProblemDetails()
    .AddBearerSecurityScheme()
    .AddEndpointsHttpSecuritySchemeResolution()
    .AddTagDescriptionSupport()
    .AddScalarTransformers()
);

builder.Services.AddHostedService<ScoreSyncService>();

builder.Services.AddEndpoints<Program>(builder.Configuration);
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapDefaultEndpoints()
    .MapEndpoints<Program>();

app.MapHangfireDashboard(new DashboardOptions
{
    Authorization = [new HangFireDashboardAuthorizationFilter()]
});

app.MapScalarApiReference("/", options => options
    .WithTitle("GuildSaber's Api")
    .WithTheme(ScalarTheme.Purple)
    .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch)
    .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
    .WithPersistentAuthentication()
);

app.Run();