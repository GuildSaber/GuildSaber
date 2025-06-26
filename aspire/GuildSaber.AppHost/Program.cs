using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mariaDb = builder.AddMySql("mariaDB")
    .WithImage("library/mariadb", "10.6.18")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
mariaDb.WithPhpMyAdmin(option => option.WithParentRelationship(mariaDb), "phpmyadmin");

var guildsaberDb = mariaDb.AddDatabase("server-db");
var discordbotDb = mariaDb.AddDatabase("discordbot-db");

var cache = builder.AddRedis("cache");
cache.WithRedisCommander(action => action.WithParentRelationship(cache), "commander");

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
    .WithReference(cache).WaitFor(cache)
    .WithReference("beatleader-api", new Uri("https://api.beatleader.com/"))
    .WithReference("scoresaber-api", new Uri("https://api.scoresaber.com/"));

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    apiService.WithHttpEndpoint(port: 5033);

builder.AddProject<GuildSaber_DiscordBot>("discord-bot", option => option.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithReference(discordbotDb).WaitFor(migrator)
    .WithReference(apiService).WaitFor(apiService)
    .WithReference(cache).WaitFor(cache)
    .WithExplicitStart();

builder.Build().Run();