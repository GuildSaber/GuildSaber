namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct WeightingSettings(
    bool IsEnabled,
    double Multiplier,
    int? TopScoreCountToConsider
)
{
    public static readonly WeightingSettings Default = new()
    {
        IsEnabled = false,
        Multiplier = 0.95d,
        TopScoreCountToConsider = null
    };
}