namespace GuildSaber.Api.Features.Guilds;

public static class GuildRequests
{
    public enum EGuildSorters
    {
        Id = 0,
        Popularity = 1,
        Name = 2,
        CreationDate = 3,
        MapCount = 4,
        MemberCount = 5
    }

    public record GuildRequirements(
        bool RequireSubmission,
        uint? MinRank,
        uint? MaxRank,
        uint? MinPP,
        uint? MaxPP,
        uint? AccountAgeUnix
    );

    public record GuildInfo(
        string Name,
        string SmallName,
        string Description,
        int Color
    );

    public record Guild(
        GuildInfo Info,
        GuildRequirements Requirements
    );
}