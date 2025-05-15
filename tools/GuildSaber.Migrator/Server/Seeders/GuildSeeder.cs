using System.Drawing;
using GuildSaber.Common.Result;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
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
            Requirements = new GuildJoinRequirements
            {
                Flags = GuildJoinRequirements.Requirements.Submission
            },
            Contexts =
            [
                new GuildContext
                {
                    Id = new GuildContext.GuildContextId(1),
                    Type = GuildContext.ContextType.Default,
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
                            Info = new PointInfo
                            {
                                Name = "CPP",
                                Description = "Challenge Pass points. The amount you get per valid map pass is fixed,"
                                              + "and the total amount you get per map on your profile exponentially decrease."
                            },
                            CurveSettings = CurveSettings.Default,
                            WeightingSettings = WeightingSettings.Default
                        }
                    ]
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
            Requirements = new GuildJoinRequirements
            {
                Flags = GuildJoinRequirements.Requirements.None
            },
            Contexts =
            [
                new GuildContext
                {
                    Id = new GuildContext.GuildContextId(2),
                    Type = GuildContext.ContextType.Default,
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
                            Info = new PointInfo
                            {
                                Name = "RPL",
                                Description =
                                    "I've heard that it once had a signification. The amount you get per valid map pass is fixed,"
                                    + "and the total amount you get per map on your profile exponentially decrease."
                            },
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