using GuildSaber.Database.Models.Server.Guilds.Points;

namespace GuildSaber.Api.Features.Scores.Calculations;

/// <summary>Fixed legacy AccStar metadata calibration, independent of normalized point bonus settings.</summary>
internal static class LegacyAccuracyCalibration
{
    public static readonly CustomCurve Curve = new([
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
}
