using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var mySqlConnectionString = builder.AddConnectionString("guildsaber-db");

var apiService = builder.AddProject<GuildSaber_Api>("api", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithHttpsEndpoint()
    .WithHttpsHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(mySqlConnectionString);

builder.AddProject<GuildSaber_DiscordBot>("DiscordBot", option => option.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();