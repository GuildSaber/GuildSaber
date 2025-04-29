using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var mySqlConnectionString = builder.AddConnectionString("guildsaber-db");

var apiService = builder.AddProject<GuildSaber_Api>("api")
    .WithHttpsEndpoint()
    .WithHttpsHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(mySqlConnectionString);

builder.AddProject<GuildSaber_DiscordBot>("DiscordBot")
    .WithReference(apiService)
    .WithHttpsEndpoint()
    .WithHttpsHealthCheck()
    .WaitFor(apiService);

builder.Build().Run();