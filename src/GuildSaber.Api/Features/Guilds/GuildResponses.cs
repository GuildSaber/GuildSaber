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
        GuildContext[] Contexts,
        PointLite[] PointsLite,
        CategoryResponses.Category[] Categories
    );

    public readonly record struct PointLite(
        uint Id,
        uint GuildContextId,
        string Name
    );

    public readonly record struct GuildContext(
        uint Id,
        EContextType Type,
        GuildContextInfo Info,
        uint[] CategoryIds,
        uint[] PointIds
    );

    public readonly record struct GuildContextInfo(
        string Name,
        string Description
    );

    public enum EContextType : byte
    {
        Default = 0,
        Tournament = 1 << 0,
        Temporary = 1 << 1
    }
}