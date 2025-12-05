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

    public record CreateGuild(
        GuildResponses.GuildInfo Info,
        GuildResponses.GuildRequirements Requirements
    );
}