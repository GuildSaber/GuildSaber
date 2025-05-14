using System.ComponentModel.DataAnnotations;

namespace GuildSaber.DiscordBot.Core.Options;

public class DiscordBotOptions
{
    public const string DiscordBotOptionsSectionsKey = "DiscordBotOptions";

    [Required] public required ulong Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string Status { get; init; }
    [Required] public required string Token { get; init; }
    [Required] public required ulong ManagerId { get; init; }
    [Required] public required ulong GuildId { get; init; }
}