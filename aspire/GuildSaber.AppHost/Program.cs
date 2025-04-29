var builder = DistributedApplication.CreateBuilder(args);
var mySqlConnectionString = builder.AddConnectionString("mysqldb");

var apiService = builder.AddProject<Projects.GuildSaber_Api>("api")
    .WithHttpsHealthCheck("/health")
    .WithReference(mySqlConnectionString);

builder.AddProject<Projects.GuildSaber_DiscordBot>("DiscordBot")
    .WithReference(mySqlConnectionString)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();