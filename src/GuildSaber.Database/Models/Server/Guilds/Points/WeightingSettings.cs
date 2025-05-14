namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct WeightingSettings(
    bool IsEnabled,
    double Multiplier = 0.95d,
    int? TopScoreCountToConsider = null
);