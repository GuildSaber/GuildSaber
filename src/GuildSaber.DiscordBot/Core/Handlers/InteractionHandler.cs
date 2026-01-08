using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GuildSaber.DiscordBot.Core.TypeConverters;
using GuildSaber.DiscordBot.Settings;
using Microsoft.Extensions.Options;

namespace GuildSaber.DiscordBot.Core.Handlers;

/// <summary>
/// Registers all the slash commands modules and handles the execution of the commands.
/// </summary>
/// <param name="client"></param>
/// <param name="commands"></param>
/// <param name="services"></param>
public class InteractionHandler(
    DiscordSocketClient client,
    InteractionService commands,
    IServiceProvider services,
    IOptions<LinkSettings> websiteSettings)
{
    private const string WebsiteIdentifier = "Website";

    public class GuildMissingException() : Exception("This discord server hasn't been registered in guild yet.");

    public class PlayerNotFoundException() : Exception(
        $"The specified player was not found, did they make an account and linked their discord on the {WebsiteIdentifier}?");

    public class CurrentPlayerNotRegisteredException() : Exception(
        $"Your discord account isn't linked on GuildSaber, please login and link it first on the {WebsiteIdentifier}.");

    public class PlayerIsNotInGuildContextException() : Exception(
        $"The specified player hasn't joined this guild context yet, they need to join the guild context from " +
        $"the {WebsiteIdentifier} in order to use this command.");

    public class CurrentPlayerDidNotJoinGuildContextException() : Exception(
        "You haven't joined the guild context yet, please join the guild context from the " +
        $"{WebsiteIdentifier} in order to use this command.");

    public async Task InitializeAsync()
    {
        commands.AddTypeConverter<GuildId>(new GuildIdTypeConverter());
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
        if (interaction.Type is not InteractionType.ApplicationCommand)
            return;

        var message = innerException?.Message ?? "An unknown error occurred.";
        var embed = innerException switch
        {
            PlayerNotFoundException => new EmbedBuilder
            {
                Title = "Player Not Found",
                Description = message.Replace(
                    WebsiteIdentifier,
                    $"[Website]({websiteSettings.Value.WebsiteBaseUri})"),
                Color = Color.Orange
            },
            CurrentPlayerNotRegisteredException => new EmbedBuilder
            {
                Title = "Whoops! (You are not registered)",
                Description = message.Replace(
                    WebsiteIdentifier,
                    $"[Website]({websiteSettings.Value.WebsiteBaseUri})"),
                Color = Color.DarkOrange
            },
            PlayerIsNotInGuildContextException => new EmbedBuilder
            {
                Title = "Player isn't in this guild context yet",
                Description = message.Replace(
                    WebsiteIdentifier,
                    $"[Website]({websiteSettings.Value.WebsiteBaseUri})"),
                Color = Color.Orange
            },
            CurrentPlayerDidNotJoinGuildContextException => new EmbedBuilder
            {
                Title = "You haven't joined the guild context yet",
                Description = message.Replace(
                    WebsiteIdentifier,
                    $"[Website]({websiteSettings.Value.WebsiteBaseUri})"),
                Color = Color.Orange
            },
            GuildMissingException when interactionContext.Guild is null => new EmbedBuilder
            {
                Title = "Not allowed outside of a guild/server",
                Description = "This command can only be used within a discord guild/server.",
                Color = Color.Orange
            },
            GuildMissingException => new EmbedBuilder
            {
                Title = "No guild is associated with this discord server",
                Description = message,
                Color = Color.Orange
            },
            _ => new EmbedBuilder
            {
                Title = "Error",
                Description = message,
                Color = Color.Red
            }
        };

        await (interaction.HasResponded
            ? interaction.FollowupAsync(embed: embed.Build())
            : interaction.RespondAsync(embed: embed.Build()));
    }
}