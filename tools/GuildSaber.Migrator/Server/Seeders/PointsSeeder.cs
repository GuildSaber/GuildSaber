using GuildSaber.Common.Result;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class PointsSeeder
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

    public static async Task SeedAsync(ServerDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Points.AnyAsync(cancellationToken))
            return;

        dbContext.Points.AddRange(new Point
        {
            Id = new Point.PointId(1),
            GuildId = new GuildId(1),
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
        }, new Point
        {
            Id = new Point.PointId(2),
            GuildId = new GuildId(2),
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
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}