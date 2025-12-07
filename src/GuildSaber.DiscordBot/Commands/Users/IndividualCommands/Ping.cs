using System.Net.NetworkInformation;
using Discord;
using Discord.Interactions;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    [SlashCommand("ping", "Get the bot ping from discord")]
    public async Task Ping() => await RespondAsync(embed: PingCommand.GetPing());
}

/// <summary>
/// Static class used to hold functions for the current command.
/// </summary>
file static class PingCommand
{
    public static Embed GetPing()
    {
        var embedBuilder = new EmbedBuilder();
        embedBuilder.AddField("Discord: ", new Ping().Send("discord.com").RoundtripTime + "ms");

        embedBuilder.WithColor(Color.Blue);
        return embedBuilder.Build();
    }
}