using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);

postgres.WithPgWeb(option => option
        .WithParentRelationship(postgres)
        .WithLifetime(ContainerLifetime.Persistent),
    "pgweb");

var guildsaberDb = postgres.AddDatabase("server-db", "server-db");
var discordbotDb = postgres.AddDatabase("discordbot-db", "discordbot-db");

/*var cache = builder.AddRedis("cache");
cache.WithRedisCommander(action => action
        .WithParentRelationship(cache),
    "commander");*/

var migrator = builder.AddProject<GuildSaber_Migrator>("migrator", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithReference(guildsaberDb).WaitFor(guildsaberDb)
    .WithReference(discordbotDb).WaitFor(discordbotDb);

var apiService = builder.AddProject<GuildSaber_Api>("api", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithHttpsEndpoint(port: 7149)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(guildsaberDb).WaitForCompletion(migrator)
    //.WithReference(cache).WaitFor(cache)
    .WithReference("beatleader-api", new Uri("https://api.beatleader.com/"))
    .WithReference("beatsaver-api", new Uri("https://api.beatsaver.com/"))
    .WithReference("scoresaber-api", new Uri("https://scoresaber.com/"))
    .WithReference("beatleader-socket", new Uri("wss://sockets.api.beatleader.com/"));

//For later: DevTunnels seems cool for exposing test urls. 

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    apiService.WithHttpEndpoint(port: 5033);

builder.AddProject<GuildSaber_DiscordBot>("discord-bot", option => option.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithReference(discordbotDb).WaitFor(migrator)
    .WithReference(apiService).WaitFor(apiService)
    //.WithReference(cache).WaitFor(cache)
    .WithParentRelationship(apiService)
    .WithExplicitStart();

builder.Build().Run();