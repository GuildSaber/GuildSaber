using GuildSaber.Database.Contexts;
using GuildSaber.DiscordBot;
using GuildSaber.DiscordBot.Core.Host;
using GuildSaber.DiscordBot.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();
builder.AddMySqlDbContext<GSDbContext>("mysqldb");
builder.Services.AddDiscordBotServices();

builder.Services.AddOptionsWithValidateOnStart<DiscordBotOptions>()
    .Bind(builder.Configuration.GetSection(Constants.DiscordBotOptionsSectionsKey))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<DiscordBotHost>();

await builder.Build().RunAsync();