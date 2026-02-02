namespace GuildSaber.Common.Services.LegacyGuildSaber.Models;

public class Metadata
{
    public required int Page { get; set; }
    public required int MaxPage { get; set; }
    public required int CountPerPage { get; set; }
    public required int TotalCount { get; set; }
}