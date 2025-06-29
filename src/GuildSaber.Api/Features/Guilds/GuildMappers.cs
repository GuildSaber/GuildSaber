using System.Linq.Expressions;
using GuildSaber.Api.Features.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;

namespace GuildSaber.Api.Features.Guilds;

public static class GuildMappers
{
    public static Expression<Func<Guild, GuildResponses.Guild>> MapGuildExpression
        => self => new GuildResponses.Guild(
            self.Id,
            new GuildResponses.GuildInfo
            {
                Name = self.Info.Name,
                SmallName = self.Info.SmallName,
                Description = self.Info.Description,
                Color = self.Info.Color.ToArgb(),
                CreatedAt = self.Info.CreatedAt
            },
            new GuildResponses.GuildRequirements
            {
                RequireSubmission = self.Requirements.RequireSubmission,
                MinRank = self.Requirements.MinRank,
                MaxRank = self.Requirements.MaxRank,
                MinPP = self.Requirements.MinPP,
                MaxPP = self.Requirements.MaxPP,
                AccountAgeUnix = self.Requirements.AccountAgeUnix
            },
            self.Status.Map()
        );

    public static Expression<Func<Guild, GuildResponses.GuildExtended>> MapGuildExtendedExpression
        => self => new GuildResponses.GuildExtended(
            new GuildResponses.Guild(
                self.Id,
                new GuildResponses.GuildInfo
                {
                    Name = self.Info.Name,
                    SmallName = self.Info.SmallName,
                    Description = self.Info.Description,
                    Color = self.Info.Color.ToArgb(),
                    CreatedAt = self.Info.CreatedAt
                },
                new GuildResponses.GuildRequirements
                {
                    RequireSubmission = self.Requirements.RequireSubmission,
                    MinRank = self.Requirements.MinRank,
                    MaxRank = self.Requirements.MaxRank,
                    MinPP = self.Requirements.MinPP,
                    MaxPP = self.Requirements.MaxPP,
                    AccountAgeUnix = self.Requirements.AccountAgeUnix
                },
                self.Status.Map()),
            self.Points.Select(p => new GuildResponses.PointLite
            {
                Id = p.Id,
                GuildId = p.GuildId,
                Name = p.Info.Name
            }).ToArray(),
            self.Categories.Select(c => new CategoryResponses.Category
            {
                Id = c.Id,
                GuildId = c.GuildId,
                Info = new CategoryResponses.CategoryInfo
                {
                    Name = c.Info.Name,
                    Description = c.Info.Description
                }
            }).ToArray()
        );

    public static Expression<Func<Point, GuildResponses.PointLite>> MapPointLiteExpression
        => self => new GuildResponses.PointLite
        {
            Id = self.Id,
            GuildId = self.GuildId,
            Name = self.Info.Name
        };

    public static GuildResponses.Guild Map(this Guild self) => new(
        self.Id,
        new GuildResponses.GuildInfo
        {
            Name = self.Info.Name,
            SmallName = self.Info.SmallName,
            Description = self.Info.Description,
            Color = self.Info.Color.ToArgb(),
            CreatedAt = self.Info.CreatedAt
        },
        new GuildResponses.GuildRequirements
        {
            RequireSubmission = self.Requirements.RequireSubmission,
            MinRank = self.Requirements.MinRank,
            MaxRank = self.Requirements.MaxRank,
            MinPP = self.Requirements.MinPP,
            MaxPP = self.Requirements.MaxPP,
            AccountAgeUnix = self.Requirements.AccountAgeUnix
        },
        self.Status.Map()
    );

    public static GuildResponses.GuildRequirements Map(this GuildRequirements self) => new()
    {
        RequireSubmission = self.RequireSubmission,
        MinRank = self.MinRank,
        MaxRank = self.MaxRank,
        MinPP = self.MinPP,
        MaxPP = self.MaxPP,
        AccountAgeUnix = self.AccountAgeUnix
    };

    public static GuildRequirements Map(this GuildRequests.CreateGuildRequirements self) => new()
    {
        RequireSubmission = self.RequireSubmission,
        MinRank = self.MinRank,
        MaxRank = self.MaxRank,
        MinPP = self.MinPP,
        MaxPP = self.MaxPP,
        AccountAgeUnix = self.AccountAgeUnix
    };

    public static GuildResponses.EGuildStatus Map(this Guild.EGuildStatus self) => self switch
    {
        Guild.EGuildStatus.Unverified => GuildResponses.EGuildStatus.Unverified,
        Guild.EGuildStatus.Verified => GuildResponses.EGuildStatus.Verified,
        Guild.EGuildStatus.Featured => GuildResponses.EGuildStatus.Featured,
        Guild.EGuildStatus.Private => GuildResponses.EGuildStatus.Private,
        _ => throw new ArgumentOutOfRangeException(nameof(self), self, null)
    };
}