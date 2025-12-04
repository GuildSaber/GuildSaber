using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.Common.Result;

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
        commands.SlashCommandExecuted += GlobalExceptionHandler;
    }

    private Task HandleInteraction(SocketInteraction interaction)
        => commands.ExecuteCommandAsync(new SocketInteractionContext(client, interaction), services);

    private async Task GlobalExceptionHandler(
        SlashCommandInfo slashCommandInfo, IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess || result is not ExecuteResult { Exception.InnerException: var innerException })
            return;

        var interaction = interactionContext.Interaction;

        switch (innerException)
        {
            case RustExtensions.ErrorException unwrapException:
            {
                logger.LogError("[HandleInteraction] {unwrapException}", unwrapException);
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.RespondAsync(embed: new EmbedBuilder
                    {
                        Title = "Error",
                        Description = unwrapException.Message,
                        Color = Color.Red
                    }.Build());
                break;
            }
            default:
            {
                logger.LogError("[HandleInteraction] {exception}", innerException);

                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist.
                // It is a good idea to delete the original response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(async msg => await msg.Result.DeleteAsync());
                break;
            }
        }
    }
}