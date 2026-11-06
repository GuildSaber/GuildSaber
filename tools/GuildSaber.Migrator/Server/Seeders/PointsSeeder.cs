using GuildSaber.Common.Result;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using Microsoft.EntityFrameworkCore;

namespace GuildSaber.Migrator.Server.Seeders;

public static class PointsSeeder
{
    public static readonly CustomCurve DefaultDiffCurve = new([
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

    public static readonly CustomCurve DefaultAccCurve = new([(1, 1), (0, 0)]);

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
                Accuracy = new CustomCurve([]),
                AccuracyBonusRatio = 0
            },
            WeightingSettings = WeightingSettings.Default
        }, new Point
        {
            Id = new Point.PointId(2),
            GuildId = new GuildId(1),
            Info = PointInfo.TryCreate(
                name: "CAP",
                description: "Challenge Accuracy points. Difficulty points plus an accuracy bonus "
                             + "calibrated from the best passes on each map."
            ).Unwrap(),
            CurveSettings = new CurveSettings
            {
                Difficulty = DefaultDiffCurve,
                Accuracy = DefaultAccCurve,
                AccuracyBonusRatio = 0.25,
                MapCurvePolicy = PointCurvePolicy.ScoreDerivedMapCurve,
                RunnerUpRewardFractions = [0.8, 0.6]
            },
            WeightingSettings = WeightingSettings.Default
        }, new Point
        {
            Id = new Point.PointId(3),
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
                Accuracy = new CustomCurve([]),
                AccuracyBonusRatio = 0
            },
            WeightingSettings = WeightingSettings.Default
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}