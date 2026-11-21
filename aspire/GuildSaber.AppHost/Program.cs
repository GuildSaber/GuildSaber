using Aspire.Hosting.Docker.Resources.ComposeNodes;

var builder = DistributedApplication.CreateBuilder(args);
var environment = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("ASPNETCORE-ENVIRONMENT",
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    : builder.AddParameter("ASPNETCORE-ENVIRONMENT");

builder.AddDockerComposeEnvironment("guildsaber-env")
    .WithDashboard(enabled: false)
    .ConfigureComposeFile(composeFile => composeFile
        // Hosted using dokploy, we must add it to the dokploy-network for traefik routing.
        .AddNetwork(new Network
        {
            Name = "dokploy-network",
            Driver = "overlay",
            External = true
        }));

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("19.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false)
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Restart = "unless-stopped";
        service.Ports = ["5432:{DB_HOST_PORT}"];
        service.Expose = [];
    });

postgres.WithPgWeb(option => option
        .WithParentRelationship(postgres)
        .WithLifetime(ContainerLifetime.Persistent),
    "pgweb");

var guildsaberDb = postgres.AddDatabase("server-db", "server-db");
var discordbotDb = postgres.AddDatabase("discordbot-db", "discordbot-db");

var migrator = builder.AddDotnetProject("migrator", "../../tools/GuildSaber.Migrator",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
    .WithReference(guildsaberDb).WaitFor(guildsaberDb)
    .WithReference(discordbotDb).WaitFor(discordbotDb)
    .PublishAsDockerComposeService((_, service) => service.PullPolicy = "always");

var apiKey = builder.AddParameter("api-key", builder.Configuration["ApiKey"]!, secret: true);
var apiService = builder.AddDotnetProject("api", "../../src/GuildSaber.Api",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
    .WithEnvironment("AuthSettings:ApiKey:Key", apiKey)
    .WithReference(guildsaberDb).WaitForCompletion(migrator)
    .WithReference("beatleader-api", new Uri("https://api.beatleader.com/"))
    .WithReference("beatsaver-api", new Uri("https://api.beatsaver.com/"))
    .WithReference("scoresaber-api", new Uri("https://scoresaber.com/"))
    .WithReference("beatleader-socket", new Uri("wss://sockets.api.beatleader.com/"))
    .WithHttpEndpoint(port: builder.ExecutionContext.IsRunMode ? 5042 : null, isProxied: false)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .PublishAsDockerComposeService((_, service) =>
    {
        service.PullPolicy = "always";
        service.Restart = "unless-stopped";

        // Add to dokploy-network for traefik routing
        service.Networks.Add("dokploy-network");
    });

BindParameters(apiService, ["S3Settings:Endpoint", "S3Settings:BucketName"], useServiceSettingsWhenUnset: true);
BindParameters(apiService, ["S3Settings:AccessKeyId", "S3Settings:SecretAccessKey"], secret: true,
    useServiceSettingsWhenUnset: true);

var discordBot = builder.AddDotnetProject("discord-bot", "../../src/GuildSaber.DiscordBot",
        options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
    .WithEnvironment("AuthSettings:ApiKey", apiKey)
    .WithReference(discordbotDb).WaitFor(migrator)
    .WithReference(apiService).WaitFor(apiService)
    .WithParentRelationship(apiService)
    .PublishAsDockerComposeService((_, service) =>
    {
        service.PullPolicy = "always";
        service.Restart = "unless-stopped";
    });

var website = builder.AddViteApp("website", "../../src/GuildSaber.Website")
    .WithPnpm()
    .WithEndpoint("http", endpointAnnotation => endpointAnnotation.Port = 5044)
    .WithExternalHttpEndpoints()
    .WithReference(apiService).WaitFor(apiService);

apiService.PublishWithContainerFiles(website, "./wwwroot");

if (builder.ExecutionContext.IsPublishMode)
{
    foreach (var key in new[] { "LinkSettings:ApiBaseUri", "LinkSettings:WebsiteBaseUri", "LinkSettings:CdnBaseUri" })
    {
        var parameter = builder.AddParameter(key.Replace(':', '-'));
        apiService.WithEnvironment(key, parameter);
        discordBot.WithEnvironment(key, parameter);
    }

    BindParameters(apiService,
    [
        "AuthSettings:Manager:SteamIds:0",
        "AuthSettings:Manager:SteamIds:1",
        "AuthSettings:Session:ExpireAfter",
        "AuthSettings:Session:MaxSessionCount",
        "AuthSettings:SessionCookie:Issuer",
        "AuthSettings:SessionCookie:Audience",
        "AuthSettings:BeatLeader:ClientId",
        "AuthSettings:Discord:ClientId",
        "AuthSettings:Redirect:AllowedOriginUrls:0",
        "AuthSettings:Redirect:AllowedOriginUrls:1",
        "AuthSettings:Redirect:AllowedOriginUrls:2",
        "AuthSettings:Redirect:AllowedOriginUrls:3",
        "GuildSettings:Creation:RequiredSubscriptionTier",
        "GuildSettings:Creation:MaxGuildCountPerUser",
        "RankedMapSettings:DefaultSettings:MaxRankedMapCount",
        "RankedMapSettings:BoostSettings:MapCountBoosts:Tier1",
        "RankedMapSettings:BoostSettings:MapCountBoosts:Tier2",
        "RankedMapSettings:BoostSettings:MapCountBoosts:Tier3"
    ]);
    BindParameters(apiService,
    [
        "AuthSettings:SessionCookie:SigningKey",
        "AuthSettings:BeatLeader:ClientSecret",
        "AuthSettings:Discord:ClientSecret"
    ], secret: true);

    BindParameters(discordBot,
    [
        "DiscordBotOptions:Id",
        "DiscordBotOptions:Name",
        "DiscordBotOptions:Status",
        "EmojiSettings:WatchingYou",
        "EmojiSettings:NeedConfirmation",
        "EmojiSettings:Confirmed",
        "EmojiSettings:Refused",
        "EmojiSettings:Congrats",
        "EmojiSettings:Nope",
        "EmojiSettings:Sad",
        "EmojiSettings:KeepItUp",
        "EmojiSettings:Trophies:Plastic",
        "EmojiSettings:Trophies:Silver",
        "EmojiSettings:Trophies:Gold",
        "EmojiSettings:Trophies:Diamond",
        "EmojiSettings:Trophies:Ruby"
    ]);
    BindParameters(discordBot, ["DiscordBotOptions:Token"], secret: true);
}

builder.Build().Run();
return;

void BindParameters<T>(
    IResourceBuilder<T> resource, string[] keys, bool secret = false,
    bool useServiceSettingsWhenUnset = false) where T : IResourceWithEnvironment
{
    foreach (var key in keys)
    {
        var name = key.Replace(':', '-');
        if (!useServiceSettingsWhenUnset || builder.ExecutionContext.IsPublishMode ||
            !string.IsNullOrWhiteSpace(builder.Configuration[$"Parameters:{name}"]))
            resource.WithEnvironment(key, builder.AddParameter(name, secret: secret));
    }
}