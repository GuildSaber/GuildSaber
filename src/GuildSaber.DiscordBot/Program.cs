using GuildSaber.Database;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Host;
using GuildSaber.DiscordBot.Settings;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Services
    .AddOptionsWithValidateOnStart<AuthSettings>()
    .Bind(builder.Configuration.GetSection(AuthSettings.AuthSettingsSectionKey)).ValidateDataAnnotations();
builder.Services
    .AddOptionsWithValidateOnStart<EmojiSettings>()
    .Bind(builder.Configuration.GetSection(EmojiSettings.EmojiSettingsSectionKey)).ValidateDataAnnotations();

builder.AddNpgsqlDbContext<DiscordBotDbContext>(connectionName: Constants.DiscordBotDbConnectionStringKey,
    configureDbContextOptions: options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddHttpClient("GuildSaber", client =>
    {
        client.BaseAddress = new Uri("https+http://api");
        client.DefaultRequestHeaders.Add("User-Agent", "GuildSaber-Bot");
    }).UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

builder.AddServiceDefaults();

builder.Services.AddHybridCache();
builder.Services.AddDiscordBot(builder.Configuration);

var app = builder.Build();

app.Run();