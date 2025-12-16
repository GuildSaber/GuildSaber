using System.ComponentModel.DataAnnotations;

namespace GuildSaber.DiscordBot.Settings;

public class EmojiSettings
{
    public const string EmojiSettingsSectionKey = "EmojiSettings";

    [Required] public required string NeedConfirmation { get; init; }
    [Required] public required string WatchingYou { get; init; }
    [Required] public required TrophyEmojis Trophies { get; init; }
}

public class TrophyEmojis
{
    [Required] public required string Plastic { get; init; }
    [Required] public required string Silver { get; init; }
    [Required] public required string Gold { get; init; }
    [Required] public required string Diamond { get; set; }
    [Required] public required string Ruby { get; set; }
}