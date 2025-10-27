namespace GuildSaber.Common.Services.OldGuildSaber.Models.Responses;

public class RankingLevel
{
    public required bool IsObtainable;
    public required int Id { get; init; }
    public required int GuildId { get; init; }
    public required float LevelNumber { get; init; }
    public required bool UseName { get; init; }
    public required string Name { get; init; }
    public required float DefaultWeight { get; init; }
    public required ulong? DiscordRoleId { get; init; }
    public required string? Description { get; init; }
    public required int Color { get; init; }
}