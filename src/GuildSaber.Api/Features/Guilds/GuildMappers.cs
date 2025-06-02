using System.Diagnostics.CodeAnalysis;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;

namespace GuildSaber.Api.Features.Guilds;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class GuildMappers
{
    public static GuildResponses.Guild Map(this Guild self) => new()
    {
        Id = self.Id,
        Info = new GuildResponses.GuildInfo
        {
            Name = self.Info.Name,
            SmallName = self.Info.SmallName,
            Description = self.Info.Description,
            Color = self.Info.Color.ToArgb(),
            CreatedAt = self.Info.CreatedAt
        },
        Requirements = new GuildResponses.GuildJoinRequirement
        {
            MinRank = self.Requirements.MinRank,
            MaxRank = self.Requirements.MaxRank,
            MinPP = self.Requirements.MinPP,
            MaxPP = self.Requirements.MaxPP,
            AccountAgeUnix = self.Requirements.AccountAgeUnix,
            Flags = self.Requirements.Flags
        },
        Status = self.Status
    };

    public static GuildResponses.PointLite MapLite(this Point self) => new()
    {
        Id = self.Id,
        GuildId = self.GuildId,
        Name = self.Info.Name
    };

    public static GuildResponses.GuildExtended MapExtended(this Guild self) => new()
    {
        Guild = self.Map(),
        PointsLite = self.Points.Select(x => x.MapLite()).ToArray()
    };
}