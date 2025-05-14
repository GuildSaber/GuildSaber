using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.DiscordBot.Core.Handlers;

namespace GuildSaber.DiscordBot.Core.Host;

/// <summary>
/// Provides methods for building a Discord Bot instance
/// </summary>
public static class DiscordBotServiceConfigurator
{
    /// <summary>
    /// Adds services for a DiscordBot instance to the specified IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDiscordBotServices(this IServiceCollection services) => services
        .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        }))
        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        .AddSingleton<InteractionHandler>();
}