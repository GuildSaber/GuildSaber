using Discord.Interactions;
using GuildSaber.Common.Result;
using GuildSaber.DiscordBot.Core.Extensions;

namespace GuildSaber.DiscordBot.Commands.Users;

public partial class UserModuleSlash
{
    /// <remarks>Yes, we don't require any player perms to run it, because it's gonna reject if the user can't</remarks>
    [SlashCommand("setguild", "Sets this discord guild as the MainDiscordGuildId of a guild")]
    public async Task SetGuild(
        [Summary("guildId", "The guildId of the guild you want this discordGuild to be set as MainDiscordGuildId")]
        GuildId guildId)
    {
        await DeferAsync(ephemeral: true);

        var guild = await Client.Value.Guilds
            .SetDiscordGuildIdAsync(guildId, Context.Guild.Id)
            .Unwrap();

        await Cache.RemoveByTagAsync(Cache.DiscordGuildIdChangeTag);
        await FollowupAsync(
            $"Set this discord guild {Context.Guild.Id} as the MainDiscordGuildId of guild '{guild.Info.Name}' (ID: {guild.Id})"
        );
    }
}