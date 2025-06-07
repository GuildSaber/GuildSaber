using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;

namespace GuildSaber.Api.Features.Guilds;

public static class GuildMappers
{
    public static Expression<Func<Guild, GuildResponses.Guild>> MapGuildExpression
        => self => new GuildResponses.Guild
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

    public static Expression<Func<Guild, GuildResponses.GuildExtended>> MapGuildExtendedExpression
        => self => new GuildResponses.GuildExtended
        {
            Guild = new GuildResponses.Guild
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
            },
            PointsLite = self.Points.Select(p => new GuildResponses.PointLite
            {
                Id = p.Id,
                GuildId = p.GuildId,
                Name = p.Info.Name
            }).ToArray(),
            Categories = self.Categories.Select(c => new CategoryResponses.Category
            {
                Id = c.Id,
                GuildId = c.GuildId,
                Info = new CategoryResponses.CategoryInfo
                {
                    Name = c.Info.Name,
                    Description = c.Info.Description
                }
            }).ToArray()
        };

    public static Expression<Func<Point, GuildResponses.PointLite>> MapPointLiteExpression
        => self => new GuildResponses.PointLite
        {
            Id = self.Id,
            GuildId = self.GuildId,
            Name = self.Info.Name
        };
}