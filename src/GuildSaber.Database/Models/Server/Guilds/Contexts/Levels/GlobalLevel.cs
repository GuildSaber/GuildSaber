namespace GuildSaber.Database.Models.Server.Guilds.Levels;

/// <summary>
/// It's a level that applies globally, regardless of category (unless stated otherwise).
/// </summary>
public sealed class GlobalLevel : Level
{
    public bool IsIgnoredInCategories { get; set; }
}