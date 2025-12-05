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
        logger.LogError("[HandleInteraction] {innerException}", innerException);
        if (interaction.Type is InteractionType.ApplicationCommand)
        {
            var embed = new EmbedBuilder
            {
                Title = "Error",
                Description = innerException?.Message ?? "An unknown error occurred.",
                Color = Color.Red
            }.Build();

            if (interaction.HasResponded)
                await interaction.FollowupAsync(embed: embed);
            else
                await interaction.RespondAsync(embed: embed);
        }
    }
}