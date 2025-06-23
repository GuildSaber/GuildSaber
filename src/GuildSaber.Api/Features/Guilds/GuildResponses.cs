using GuildSaber.Api.Features.Guilds.Categories;

namespace GuildSaber.Api.Features.Guilds;

public static class GuildResponses
{
    public readonly record struct GuildInfo(
        string Name,
        string SmallName,
        string Description,
        int Color,
        DateTimeOffset CreatedAt
    );

    public readonly record struct GuildRequirements(
        bool RequireSubmission,
        uint? MinRank,
        uint? MaxRank,
        uint? MinPP,
        uint? MaxPP,
        uint? AccountAgeUnix
    );

    public record Guild(
        uint Id,
        GuildInfo Info,
        GuildRequirements Requirements,
        EGuildStatus Status
    );

    public enum EGuildStatus : byte
    {
        Unverified = 0,
        Verified = 1,
        Featured = 2,
        Private = 3
    }

    public record GuildExtended(
        Guild Guild,
        PointLite[] PointsLite,
        CategoryResponses.Category[] Categories
    );

    public readonly record struct PointLite(
        uint Id,
        ulong GuildId,
        string Name
    );
}