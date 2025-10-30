namespace GuildSaber.Api.Features.Guilds;

public static class GuildRequests
{
    public enum EGuildSorter
    {
        Id = 0,
        Popularity = 1,
        Name = 2,
        CreationDate = 3,
        MapCount = 4,
        MemberCount = 5
    }

    public record CreateGuildRequirements(
        bool RequireSubmission,
        int? MinRank,
        int? MaxRank,
        int? MinPP,
        int? MaxPP,
        int? AccountAgeUnix
    );

    public record CreateGuildInfo(
        string Name,
        string SmallName,
        string Description,
        int Color
    );

    public record CreateGuild(
        CreateGuildInfo Info,
        CreateGuildRequirements Requirements
    );
}