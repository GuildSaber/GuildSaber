using System.Drawing;
using GuildSaber.Common.Result;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;
using Point = GuildSaber.Database.Models.Server.Guilds.Points.Point;

namespace GuildSaber.Migrator.Server.Seeders;

public static class GuildSeeder
{
    public static readonly Guild[] DefaultGuilds =
    [
        new()
        {
            Id = new Guild.GuildId(1),
            Info = GuildInfo.TryCreate(
                name: "Challenge Saber",
                smallName: "CS",
                description: "A Level based ranking with category support."
                             + "Come and fight for who’s the best in Vibro, Tech, Shitpost or Jumps!",
                color: Color.FromArgb(255, 166, 19, 111),
                createdAt: DateTimeOffset.Now
            ).Unwrap(),
            Requirements = new GuildRequirements
            {
                RequireSubmission = true
            },
            Status = Guild.EGuildStatus.Featured,
            Contexts =
            [
                new GuildContext
                {
                    Id = new GuildContext.GuildContextId(1),
                    Type = GuildContext.EContextType.Default,
                    Info = new GuildContextInfo
                    {
                        Description = "The main context of the guild, where all the maps are ranked.",
                        Name = "Default"
                    },
                    Points =
                    [
                        new Point
                        {
                            Id = new Point.PointId(1),
                            GuildId = new Guild.GuildId(1),
                            Info = PointInfo.TryCreate(
                                name: "CPP",
                                description: "Challenge Pass points. The amount you get per valid map pass is fixed,"
                                             + "and the total amount you get per map on your profile exponentially decrease."
                            ).Unwrap(),
                            CurveSettings = CurveSettings.Default,
                            WeightingSettings = WeightingSettings.Default
                        }
                    ]
                }
            ],
            Categories =
            [
                new Category
                {
                    Id = new Category.CategoryId(1),
                    Info = CategoryInfo.TryCreate(
                        name: "Vibro",
                        description: "The Vibro category, for the most vibro maps."
                    ).Unwrap()
                },
                new Category
                {
                    Id = new Category.CategoryId(2),
                    Info = CategoryInfo.TryCreate(
                        name: "Tech",
                        description: "The Tech category, for the most tech maps."
                    ).Unwrap()
                },
                new Category
                {
                    Id = new Category.CategoryId(3),
                    Info = CategoryInfo.TryCreate(
                        name: "Shitpost",
                        description: "The Shitpost category, for the most shitpost maps."
                    ).Unwrap()
                },
                new Category
                {
                    Id = new Category.CategoryId(4),
                    Info = CategoryInfo.TryCreate(
                        name: "Jumps",
                        description: "The Jumps category, for the most jumps maps."
                    ).Unwrap()
                }
            ]
        },
        new()
        {
            Id = new Guild.GuildId(2),
            Info = GuildInfo.TryCreate(
                name: "Beat Saber Challenge Community",
                smallName: "BSCC",
                description: "The legacy of the challenge ranking, a higher mapping standard, a loving community."
                             + "Why waiting? Join-us!",
                color: Color.FromArgb(255, 255, 124, 0),
                createdAt: DateTimeOffset.Now
            ).Unwrap(),
            Status = Guild.EGuildStatus.Featured,
            Requirements = new GuildRequirements
            {
                RequireSubmission = false,
                MinRank = 10,
                MaxRank = 200,
                MinPP = 0,
                MaxPP = 10000,
                AccountAgeUnix = (uint?)TimeSpan.FromDays(365 * 6).Seconds
            },
            Contexts =
            [
                new GuildContext
                {
                    Id = new GuildContext.GuildContextId(2),
                    Type = GuildContext.EContextType.Default,
                    Info = new GuildContextInfo
                    {
                        Description = "The main context of the guild, where all the maps are ranked.",
                        Name = "Default"
                    },
                    Points =
                    [
                        new Point
                        {
                            Id = new Point.PointId(2),
                            GuildId = new Guild.GuildId(2),
                            Info = PointInfo.TryCreate(
                                name: "RPL",
                                description:
                                "The Beat Saber Challenge Community points. The amount you get per valid map pass is fixed,"
                                + "and the total amount you get per map on your profile exponentially decrease."
                            ).Unwrap(),
                            CurveSettings = CurveSettings.Default,
                            WeightingSettings = WeightingSettings.Default
                        }
                    ]
                }
            ]
        }
    ];

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Guilds.AnyAsync(cancellationToken))
            return;

        await dbContext.Guilds.AddRangeAsync(DefaultGuilds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}