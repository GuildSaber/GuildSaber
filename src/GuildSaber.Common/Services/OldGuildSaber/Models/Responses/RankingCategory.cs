namespace GuildSaber.Common.Services.OldGuildSaber.Models.Responses;

public class RankingCategory
{
    public required int Id { get; init; }
    public required int GuildId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Logo { get; init; }
}