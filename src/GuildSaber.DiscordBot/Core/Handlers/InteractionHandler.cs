using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace GuildSaber.DiscordBot.Core.Handlers;

/// <summary>
/// Registers all the slash commands modules and handles the execution of the commands.
/// </summary>
/// <param name="client"></param>
/// <param name="commands"></param>
/// <param name="services"></param>
/// <param name="logger"></param>
public class InteractionHandler(
    DiscordSocketClient client,
    InteractionService commands,
    IServiceProvider services,
    ILogger<InteractionHandler> logger)
{
    public async Task InitializeAsync()
    {
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        client.InteractionCreated += HandleInteraction;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);
            await commands.ExecuteCommandAsync(context, services);
        }
        catch (Exception exception)
        {
            logger.LogError("[HandleInteraction] {exception}", exception);

            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist.
            // It is a good idea to delete the original response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }
}