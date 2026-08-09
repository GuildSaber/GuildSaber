using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var environment = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("ASPNETCORE-ENVIRONMENT",
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    : builder.AddParameter("ASPNETCORE-ENVIRONMENT");

builder.AddDockerComposeEnvironment("guildsaber-env")
    .WithDashboard(enabled: false);

var postgres = builder.AddPostgres("postgres", port: 5432)
    //TODO: Update to 18.x when migration is figured out.
    .WithImageTag("17.6")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false)
    .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");

postgres.WithPgWeb(option => option
        .WithParentRelationship(postgres)
        .WithLifetime(ContainerLifetime.Persistent),
    "pgweb");

var guildsaberDb = postgres.AddDatabase("server-db", "server-db");
var discordbotDb = postgres.AddDatabase("discordbot-db", "discordbot-db");

var migrator = builder.AddProject<GuildSaber_Migrator>("migrator", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
    .WithReference(guildsaberDb).WaitFor(guildsaberDb)
    .WithReference(discordbotDb).WaitFor(discordbotDb)
    .PublishAsDockerComposeService((_, service) => service.PullPolicy = "always");

var apiKey = builder.AddParameter("api-key", builder.Configuration["ApiKey"]!, secret: true);
var apiService = builder.AddProject<GuildSaber_Api>("api", options => options.ExcludeLaunchProfile = true)
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
    });

var discordBot = builder.AddProject<GuildSaber_DiscordBot>("discord-bot", option => option.ExcludeLaunchProfile = true)
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

// Bind environment variables in publish mode (for production deployments)
if (builder.ExecutionContext.IsPublishMode)
{
    var apiBaseUri = builder.AddParameter("LinkSettings-ApiBaseUri");
    var websiteBaseUri = builder.AddParameter("LinkSettings-WebsiteBaseUri");
    var cdnBaseUri = builder.AddParameter("LinkSettings-CdnBaseUri");

    apiService
        .WithEnvironment("LinkSettings:ApiBaseUri", apiBaseUri)
        .WithEnvironment("LinkSettings:WebsiteBaseUri", websiteBaseUri)
        .WithEnvironment("LinkSettings:CdnBaseUri", cdnBaseUri)
        .WithEnvironment("AuthSettings:Manager:SteamIds:0",
            builder.AddParameter("AuthSettings-Manager-SteamIds-0"))
        .WithEnvironment("AuthSettings:Manager:SteamIds:1", builder.AddParameter("AuthSettings-Manager-SteamIds-1"))
        .WithEnvironment("AuthSettings:Session:ExpireAfter", builder.AddParameter("AuthSettings-Session-ExpireAfter"))
        .WithEnvironment("AuthSettings:Session:MaxSessionCount",
            builder.AddParameter("AuthSettings-Session-MaxSessionCount"))
        .WithEnvironment("AuthSettings:SessionCookie:Issuer",
            builder.AddParameter("AuthSettings-SessionCookie-Issuer"))
        .WithEnvironment("AuthSettings:SessionCookie:Audience",
            builder.AddParameter("AuthSettings-SessionCookie-Audience"))
        .WithEnvironment("AuthSettings:SessionCookie:SigningKey",
            builder.AddParameter("AuthSettings-SessionCookie-SigningKey", secret: true))
        .WithEnvironment("AuthSettings:BeatLeader:ClientId", builder.AddParameter("AuthSettings-BeatLeader-ClientId"))
        .WithEnvironment("AuthSettings:BeatLeader:ClientSecret",
            builder.AddParameter("AuthSettings-BeatLeader-ClientSecret", secret: true))
        .WithEnvironment("AuthSettings:Discord:ClientId", builder.AddParameter("AuthSettings-Discord-ClientId"))
        .WithEnvironment("AuthSettings:Discord:ClientSecret",
            builder.AddParameter("AuthSettings-Discord-ClientSecret", secret: true))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:0",
            builder.AddParameter("AuthSettings-Redirect-AllowedOriginUrls-0"))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:1",
            builder.AddParameter("AuthSettings-Redirect-AllowedOriginUrls-1"))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:2",
            builder.AddParameter("AuthSettings-Redirect-AllowedOriginUrls-2"))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:3",
            builder.AddParameter("AuthSettings-Redirect-AllowedOriginUrls-3"))
        .WithEnvironment("AuthSettings:TickerQ:ApiKey",
            builder.AddParameter("AuthSettings-TickerQ-ApiKey", secret: true))
        .WithEnvironment("GuildSettings:Creation:RequiredSubscriptionTier",
            builder.AddParameter("GuildSettings-Creation-RequiredSubscriptionTier"))
        .WithEnvironment("GuildSettings:Creation:MaxGuildCountPerUser",
            builder.AddParameter("GuildSettings-Creation-MaxGuildCountPerUser"))
        .WithEnvironment("RankedMapSettings:DefaultSettings:MaxRankedMapCount",
            builder.AddParameter("RankedMapSettings-DefaultSettings-MaxRankedMapCount"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier1",
            builder.AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier1"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier2",
            builder.AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier2"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier3",
            builder.AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier3"));

    discordBot.WithEnvironment("DiscordBotOptions:Id", builder.AddParameter("DiscordBotOptions-Id"))
        .WithEnvironment("DiscordBotOptions:Name", builder.AddParameter("DiscordBotOptions-Name"))
        .WithEnvironment("DiscordBotOptions:Status", builder.AddParameter("DiscordBotOptions-Status"))
        .WithEnvironment("DiscordBotOptions:Token", builder.AddParameter("DiscordBotOptions-Token", secret: true))
        .WithEnvironment("LinkSettings:ApiBaseUri", apiBaseUri)
        .WithEnvironment("LinkSettings:WebsiteBaseUri", websiteBaseUri)
        .WithEnvironment("LinkSettings:CdnBaseUri", cdnBaseUri)
        .WithEnvironment("EmojiSettings:WatchingYou", builder.AddParameter("EmojiSettings-WatchingYou"))
        .WithEnvironment("EmojiSettings:NeedConfirmation", builder.AddParameter("EmojiSettings-NeedConfirmation"))
        .WithEnvironment("EmojiSettings:Confirmed", builder.AddParameter("EmojiSettings-Confirmed"))
        .WithEnvironment("EmojiSettings:Refused", builder.AddParameter("EmojiSettings-Refused"))
        .WithEnvironment("EmojiSettings:Congrats", builder.AddParameter("EmojiSettings-Congrats"))
        .WithEnvironment("EmojiSettings:Nope", builder.AddParameter("EmojiSettings-Nope"))
        .WithEnvironment("EmojiSettings:Sad", builder.AddParameter("EmojiSettings-Sad"))
        .WithEnvironment("EmojiSettings:KeepItUp", builder.AddParameter("EmojiSettings-KeepItUp"))
        .WithEnvironment("EmojiSettings:Trophies:Plastic", builder.AddParameter("EmojiSettings-Trophies-Plastic"))
        .WithEnvironment("EmojiSettings:Trophies:Silver", builder.AddParameter("EmojiSettings-Trophies-Silver"))
        .WithEnvironment("EmojiSettings:Trophies:Gold", builder.AddParameter("EmojiSettings-Trophies-Gold"))
        .WithEnvironment("EmojiSettings:Trophies:Diamond", builder.AddParameter("EmojiSettings-Trophies-Diamond"))
        .WithEnvironment("EmojiSettings:Trophies:Ruby", builder.AddParameter("EmojiSettings-Trophies-Ruby"));
}

builder.Build().Run();
