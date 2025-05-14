namespace GuildSaber.Database.Models.DiscordBot;

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


    /// <summary>
    /// Exposed enum flag used for permission management and persistance.
    /// </summary>
    [Flags]
    public enum EPermissions
    {
        None = 0,
        Manager = 1 << 0,
        Admin = 1 << 1
    }
}