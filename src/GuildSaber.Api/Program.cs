using System.Text;
using AspNet.Security.OAuth.BeatLeader;
using GuildSaber.Api.Extensions;
using GuildSaber.Api.Features.Auth;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Api.Hangfire;
using GuildSaber.Api.Hangfire.Configuration;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Database;
using GuildSaber.Database.Contexts.Server;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyCSharp.HttpUserAgentParser.AspNetCore;
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

var connectionMultiplexer = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("cache")!);

builder.AddServiceDefaults();
builder.Services.AddHttpUserAgentParser()
    .AddHttpUserAgentParserAccessor();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddHttpClient<BeatLeaderApi>(client =>
{
    client.BaseAddress = new Uri("https+http://beatleader-api");
    client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");
});
builder.Services.AddScoped<AuthService>(services => new AuthService(
    services.GetRequiredService<JwtService>(),
    services.GetRequiredService<IOptions<SessionSettings>>(),
    services.GetRequiredService<IHttpUserAgentParserAccessor>(),
    services.GetRequiredService<BeatLeaderApi>(),
    services.GetRequiredService<ServerDbContext>())
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = BeatLeaderAuthenticationDefaults.AuthenticationScheme;
}).AddBeatLeader(options =>
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
});

builder.Services.AddAuthorization();
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
builder.Services.AddOpenApi(options => options.AddTagDescriptionSupport());
builder.Services.AddEndpoints<Program>(builder.Configuration);
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapDefaultEndpoints()
    .MapEndpoints<Program>();

app.MapHangfireDashboard(new DashboardOptions
{
    Authorization = [new HangFireDashboardAuthorizationFilter()]
});

app.MapScalarApiReference("/", options =>
{
    options.WithTitle("GuildSaber's Api")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch);
});

app.Run();