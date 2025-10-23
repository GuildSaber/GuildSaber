using GuildSaber.Database;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Host;
using GuildSaber.DiscordBot.Core.Options;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.AddNpgsqlDbContext<DiscordBotDbContext>(connectionName: Constants.DiscordBotDbConnectionStringKey,
    configureDbContextOptions: options => options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.AddServiceDefaults();
builder.Services.AddDiscordBotServices();

builder.Services.AddOptionsWithValidateOnStart<DiscordBotOptions>()
    .Bind(builder.Configuration.GetSection(DiscordBotOptions.DiscordBotOptionsSectionsKey))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<DiscordBotHost>();
await builder.Build().RunAsync();