using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("guildsaber-env")
    .WithDashboard(dashboard => dashboard
        .WithForwardedHeaders(enabled: true)
        .WithHostPort());

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

var apiKey = builder.AddParameter("api-key", builder.Configuration["ApiKey"]!, secret: true);
var apiService = builder.AddProject<GuildSaber_Api>("api", options => options.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithEnvironment("AuthSettings:ApiKey:Key", apiKey)
    .WithHttpEndpoint(port: builder.ExecutionContext.IsRunMode ? 5042 : null, isProxied: false)
    .WithReference(guildsaberDb).WaitForCompletion(migrator)
    .WithReference("beatleader-api", new Uri("https://api.beatleader.com/"))
    .WithReference("beatsaver-api", new Uri("https://api.beatsaver.com/"))
    .WithReference("scoresaber-api", new Uri("https://scoresaber.com/"))
    .WithReference("beatleader-socket", new Uri("wss://sockets.api.beatleader.com/"));


apiService
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var discordBot = builder.AddProject<GuildSaber_DiscordBot>("discord-bot", option => option.ExcludeLaunchProfile = true)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    .WithEnvironment("AuthSettings:ApiKey", apiKey)
    .WithReference(discordbotDb).WaitFor(migrator)
    .WithReference(apiService).WaitFor(apiService)
    .WithParentRelationship(apiService);

// Bind environment variables in publish mode (for production deployments)
if (builder.ExecutionContext.IsPublishMode)
{
    apiService.WithEnvironment("AuthSettings:Manager:SteamIds:0", builder
            .AddParameter("AuthSettings-Manager-SteamIds-0"))
        .WithEnvironment("AuthSettings:Manager:SteamIds:1", builder
            .AddParameter("AuthSettings-Manager-SteamIds-1"))
        .WithEnvironment("AuthSettings:Session:ExpireAfter", builder
            .AddParameter("AuthSettings-Session-ExpireAfter"))
        .WithEnvironment("AuthSettings:Session:MaxSessionCount", builder
            .AddParameter("AuthSettings-Session-MaxSessionCount"))
        .WithEnvironment("AuthSettings:Jwt:Issuer", builder
            .AddParameter("AuthSettings-Jwt-Issuer"))
        .WithEnvironment("AuthSettings:Jwt:Audience", builder
            .AddParameter("AuthSettings-Jwt-Audience"))
        .WithEnvironment("AuthSettings:Jwt:Secret", builder
            .AddParameter("AuthSettings-Jwt-Secret", secret: true))
        .WithEnvironment("AuthSettings:BeatLeader:ClientId", builder
            .AddParameter("AuthSettings-BeatLeader-ClientId"))
        .WithEnvironment("AuthSettings:BeatLeader:ClientSecret", builder
            .AddParameter("AuthSettings-BeatLeader-ClientSecret", secret: true))
        .WithEnvironment("AuthSettings:Discord:ClientId", builder
            .AddParameter("AuthSettings-Discord-ClientId"))
        .WithEnvironment("AuthSettings:Discord:ClientSecret", builder
            .AddParameter("AuthSettings-Discord-ClientSecret", secret: true))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:0", builder
            .AddParameter("AuthSettings-Redirect-AllowedOriginUrls-0"))
        .WithEnvironment("AuthSettings:Redirect:AllowedOriginUrls:1", builder
            .AddParameter("AuthSettings-Redirect-AllowedOriginUrls-1"))
        .WithEnvironment("GuildSettings:Creation:RequiredSubscriptionTier", builder
            .AddParameter("GuildSettings-Creation-RequiredSubscriptionTier"))
        .WithEnvironment("GuildSettings:Creation:MaxGuildCountPerUser", builder
            .AddParameter("GuildSettings-Creation-MaxGuildCountPerUser"))
        .WithEnvironment("RankedMapSettings:DefaultSettings:MaxRankedMapCount", builder
            .AddParameter("RankedMapSettings-DefaultSettings-MaxRankedMapCount"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier1", builder
            .AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier1"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier2", builder
            .AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier2"))
        .WithEnvironment("RankedMapSettings:BoostSettings:MapCountBoosts:Tier3", builder
            .AddParameter("RankedMapSettings-BoostSettings-MapCountBoosts-Tier3"));

    discordBot.WithEnvironment("DiscordBotOptions:Id", builder.AddParameter("DiscordBotOptions-Id"))
        .WithEnvironment("DiscordBotOptions:Name", builder.AddParameter("DiscordBotOptions-Name"))
        .WithEnvironment("DiscordBotOptions:Status", builder.AddParameter("DiscordBotOptions-Status"))
        .WithEnvironment("DiscordBotOptions:Token", builder.AddParameter("DiscordBotOptions-Token", secret: true))
        .WithEnvironment("DiscordBotOptions:GuildId", builder.AddParameter("DiscordBotOptions-GuildId"))
        .WithEnvironment("EmojiSettings:WatchingYou", builder.AddParameter("EmojiSettings-WatchingYou"))
        .WithEnvironment("EmojiSettings:NeedConfirmation", builder.AddParameter("EmojiSettings-NeedConfirmation"))
        .WithEnvironment("EmojiSettings:Trophies:Plastic", builder.AddParameter("EmojiSettings-Trophies-Plastic"))
        .WithEnvironment("EmojiSettings:Trophies:Silver", builder.AddParameter("EmojiSettings-Trophies-Silver"))
        .WithEnvironment("EmojiSettings:Trophies:Gold", builder.AddParameter("EmojiSettings-Trophies-Gold"))
        .WithEnvironment("EmojiSettings:Trophies:Diamond", builder.AddParameter("EmojiSettings-Trophies-Diamond"))
        .WithEnvironment("EmojiSettings:Trophies:Ruby", builder.AddParameter("EmojiSettings-Trophies-Ruby"));
}

builder.Build().Run();