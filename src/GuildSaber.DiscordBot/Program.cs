using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.DiscordBot.Core.Host;
using GuildSaber.DiscordBot.Core.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.AddMySqlDbContext<DiscordBotDbContext>(connectionName: DiscordBotOptions.DiscordBotOptionsSectionsKey);

builder.AddServiceDefaults();
builder.Services.AddDiscordBotServices();

builder.Services.AddOptionsWithValidateOnStart<DiscordBotOptions>()
    .Bind(builder.Configuration.GetSection(DiscordBotOptions.DiscordBotOptionsSectionsKey))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<DiscordBotHost>();
await builder.Build().RunAsync();