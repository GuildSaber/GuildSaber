using GuildSaber.Database;
using GuildSaber.Database.Contexts.DiscordBot;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Migrator;
using DiscordBot_Worker = GuildSaber.Migrator.DiscordBot.Worker;
using Server_Worker = GuildSaber.Migrator.Server.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing
    .AddSource(Server_Worker.ActivitySourceName, DiscordBot_Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<ServerDbContext>(Constants.ServerDbConnectionStringKey);
builder.AddNpgsqlDbContext<DiscordBotDbContext>(Constants.DiscordBotDbConnectionStringKey);

builder.Services.AddHostedService<Server_Worker>();
builder.Services.AddHostedService<DiscordBot_Worker>();
builder.Services.AddHostedService<HostShutdownOnMigrationWorkerStopped>();

var host = builder.Build();

host.Run();