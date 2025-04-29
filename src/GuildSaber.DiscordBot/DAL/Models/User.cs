using EPermissions = GuildSaber.DiscordBot.Core.Handlers.PermissionHandler.EPermissions;

namespace GuildSaber.DiscordBot.DAL.Models;

public class User
{
    /// <summary>
    /// The Discord user Id.
    /// </summary>
    public ulong Id { get; init; }

    /// <summary>
    /// The User command permissions flag.
    /// </summary>
    public EPermissions Permissions { get; set; }
}