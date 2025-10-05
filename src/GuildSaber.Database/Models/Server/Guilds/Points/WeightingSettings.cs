using System.ComponentModel;

namespace GuildSaber.Database.Models.Server.Guilds.Points;

/// <summary>
/// Settings for points weighting using exponential decrease.
/// The weighting follows the formula: Multiplier^(n-1) where n is the ranking of a specific play amongst all points plays.
/// </summary>
public readonly record struct WeightingSettings(
    bool IsEnabled,
    double Multiplier,
    [Description("Consider only the top X scores for points weighting. If null, consider all scores.")]
    int? TopScoreCountToConsider
)
{
    public static readonly WeightingSettings Default = new()
    {
        IsEnabled = true,
        Multiplier = 0.95d,
        TopScoreCountToConsider = null
    };
}