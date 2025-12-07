using Discord.Interactions;
using GuildSaber.Common.Result;

namespace GuildSaber.DiscordBot.Commands.Manager;

/// <remarks>
/// This class is partial because the command can only be registered in a module.
/// </remarks>
public partial class ManagerModuleSlash
{
    [SlashCommand("setguild", "Sets this discord guild as the MainDiscordGuildId of a guild")]
    public async Task SetGuild(
        [Summary("guildId", "The guildId of the guild you want this discordGuild to be set as MainDiscordGuildId")]
        int guildId)
    {
        await DeferAsync(ephemeral: true);
        var guild = await Client.Value.Guilds
            .SetDiscordGuildIdAsync(new GuildId(guildId), Context.Guild.Id, CancellationToken.None)
            .Unwrap();

        await FollowupAsync(
            $"Set this discord guild {Context.Guild.Id} as the MainDiscordGuildId of guild '{guild.Info.Name}' (ID: {guild.Id})"
        );
    }
}