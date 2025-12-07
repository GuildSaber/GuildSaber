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
    public class GuildMissingException() : Exception("This discord server hasn't been registered in guild yet.");

    public class PlayerNotFoundException() : Exception(
        "Player with the specified discord ID was not found in the guild, did they link their discord account?");

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

        logger.LogError("[HandleInteraction] {innerException}", innerException);

        var interaction = interactionContext.Interaction;
        if (interaction.Type is not InteractionType.ApplicationCommand)
            return;

        var embed = innerException switch
        {
            PlayerNotFoundException => new EmbedBuilder
            {
                Title = "Player Not Found",
                Description = innerException.Message,
                Color = Color.Orange
            },
            GuildMissingException _ when interactionContext.Guild is null => new EmbedBuilder
            {
                Title = "Guild Not Found",
                Description = "This command can only be used in a guild (server) context.",
                Color = Color.Orange
            },
            GuildMissingException => new EmbedBuilder
            {
                Title = "Guild Not Found",
                Description = innerException.Message,
                Color = Color.Orange
            },
            _ => new EmbedBuilder
            {
                Title = "Error",
                Description = innerException?.Message ?? "An unknown error occurred.",
                Color = Color.Red
            }
        };

        await (interaction.HasResponded
            ? interaction.FollowupAsync(embed: embed.Build())
            : interaction.RespondAsync(embed: embed.Build()));
    }
}