using GuildSaber.Database.Models.Server.Guilds;

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

    public readonly record struct GuildJoinRequirement(
        GuildJoinRequirements.Requirements Flags,
        uint MinRank,
        uint MaxRank,
        uint MinPP,
        uint MaxPP,
        uint AccountAgeUnix
    );

    public readonly record struct Guild(
        ulong Id,
        GuildInfo Info,
        GuildJoinRequirement Requirements,
        Database.Models.Server.Guilds.Guild.EGuildStatus Status
    );

    public readonly record struct GuildExtended(
        Guild Guild,
        PointLite[] PointsLite,
        Category[] Categories
    );

    public readonly record struct PointLite(
        int Id,
        ulong GuildId,
        string Name
    );

    public readonly record struct Category(
        ulong Id,
        ulong GuildId,
        string Name,
        string? Description
    );
}