using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.Core.Options;

namespace GuildSaber.DiscordBot.Core.Host;

/// <summary>
/// Provides methods for building a Discord Bot instance
/// </summary>
public static class DiscordBotServiceConfigurator
{
    /// <summary>
    /// Adds services for a DiscordBot instance to the specified IServiceCollection.
    /// </summary>
    public static IServiceCollection AddDiscordBot(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            }))
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>();

        services.AddOptionsWithValidateOnStart<DiscordBotOptions>()
            .Bind(configuration.GetSection(DiscordBotOptions.DiscordBotOptionsSectionsKey))
            .ValidateDataAnnotations();

        services.AddHostedService<DiscordBotHost>();

        return services;
    }
}