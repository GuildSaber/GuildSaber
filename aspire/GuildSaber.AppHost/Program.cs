using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env")
    .WithDashboard(dashboard =>
    {
        dashboard.WithHostPort(8080)
            .WithForwardedHeaders(enabled: true);
    });

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);

postgres.WithPgWeb(option => option
        .WithParentRelationship(postgres)
        .WithLifetime(ContainerLifetime.Persistent),
    "pgweb");

var guildsaberDb = postgres.AddDatabase("server-db", "server-db");
var discordbotDb = postgres.AddDatabase("discordbot-db", "discordbot-db");

var migrator = builder.AddProject<GuildSaber_Migrator>("migrator", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithReference(guildsaberDb).WaitFor(guildsaberDb)
    .WithReference(discordbotDb).WaitFor(discordbotDb);

var apiKey = builder.AddParameter("api-key", Convert.ToBase64String(Guid.NewGuid().ToByteArray()), secret: true);
var apiService = builder.AddProject<GuildSaber_Api>("api", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithEnvironment("AuthSettings:ApiKey:Key", apiKey)
    .WithHttpsEndpoint(port: 7149)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(guildsaberDb).WaitForCompletion(migrator)
    .WithReference("beatleader-api", new Uri("https://api.beatleader.com/"))
    .WithReference("beatsaver-api", new Uri("https://api.beatsaver.com/"))
    .WithReference("scoresaber-api", new Uri("https://scoresaber.com/"))
    .WithReference("beatleader-socket", new Uri("wss://sockets.api.beatleader.com/"));

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    apiService.WithHttpEndpoint(port: 5033);

builder.AddProject<GuildSaber_DiscordBot>("discord-bot", option => option.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithEnvironment("AuthSettings:ApiKey", apiKey)
    .WithReference(discordbotDb).WaitFor(migrator)
    .WithReference(apiService).WaitFor(apiService)
    .WithParentRelationship(apiService)
    .WithExplicitStart();

builder.Build().Run();