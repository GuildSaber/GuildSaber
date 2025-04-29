using Discord.Interactions;
using GuildSaber.DiscordBot.Core.Handlers;
using GuildSaber.DiscordBot.DAL.Models;

namespace GuildSaber.DiscordBot.Commands.Manager;

/// <remarks>
/// This class is partial because the command can only be registered in a module.
/// </remarks>
public partial class ManagerModuleSlash
{
    [SlashCommand("print_managers_id", "Print the current managers id")]
    public async Task PrintManagersId() => await RespondAsync(
        text: Queryable
            .Where<User>(dbContext.Users, x => x.Permissions.HasFlag(PermissionHandler.EPermissions.Manager))
            .Select(x => x.Id.ToString()).ToList()
            .Aggregate((x, y) => $"{x}, {y}")
    );
}