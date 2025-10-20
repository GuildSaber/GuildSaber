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
    public static CustomCurve DefaultDiffCurve = new([
        (32.0, 1746.000),
        (31.0, 1352.000),
        (30.0, 1131.250),
        (29.0, 1009.563),
        (28.0, 892.000),
        (27.0, 778.563),
        (26.0, 669.250),
        (25.0, 564.063),
        (24.0, 463.000),
        (23.0, 366.063),
        (22.0, 273.250),
        (21.0, 184.563),
        (20.0, 100.000),
        (19.0, 90.033),
        (18.0, 80.262),
        (17.0, 70.769),
        (16.0, 62.005),
        (15.0, 53.788),
        (14.0, 46.295),
        (13.0, 39.563),
        (12.0, 33.596),
        (11.0, 28.370),
        (10.0, 23.841),
        (9.0, 19.950),
        (8.0, 16.345),
        (7.0, 13.828),
        (6.0, 11.464),
        (5.0, 9.485),
        (4.0, 7.833),
        (3.0, 6.459),
        (2.0, 5.319),
        (1.0, 4.376),
        (0.0, 0.0)
    ]);

    public static CustomCurve DefaultAccCurve = new([
        (1.0, 7.424),
        (0.999, 6.241),
        (0.9975, 5.158),
        (0.995, 4.010),
        (0.9925, 3.241),
        (0.99, 2.700),
        (0.9875, 2.303),
        (0.985, 2.007),
        (0.9825, 1.786),
        (0.98, 1.618),
        (0.9775, 1.490),
        (0.975, 1.392),
        (0.9725, 1.315),
        (0.97, 1.256),
        (0.965, 1.167),
        (0.96, 1.094),
        (0.955, 1.039),
        (0.95, 1.000),
        (0.94, 0.931),
        (0.93, 0.867),
        (0.92, 0.813),
        (0.91, 0.768),
        (0.9, 0.729),
        (0.875, 0.650),
        (0.85, 0.581),
        (0.825, 0.522),
        (0.8, 0.473),
        (0.75, 0.404),
        (0.7, 0.345),
        (0.65, 0.296),
        (0.6, 0.256),
        (0.0, 0.000)
    ]);

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
                createdAt: DateTimeOffset.UtcNow
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
                            CurveSettings = new CurveSettings
                            {
                                Difficulty = DefaultDiffCurve,
                                Accuracy = DefaultAccCurve
                            },
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
                createdAt: DateTimeOffset.UtcNow
            ).Unwrap(),
            Status = Guild.EGuildStatus.Featured,
            Requirements = new GuildRequirements
            {
                RequireSubmission = false,
                MinRank = 200,
                MaxRank = 10,
                MinPP = 0,
                MaxPP = 10000,
                AccountAgeUnix = TimeSpan.FromDays(365 * 6).Seconds
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
                            CurveSettings = new CurveSettings
                            {
                                Difficulty = DefaultDiffCurve,
                                Accuracy = DefaultAccCurve
                            },
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