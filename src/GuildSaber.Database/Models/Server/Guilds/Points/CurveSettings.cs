namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct CurveSettings(
    float Slope = 0.0f,
    float SlopeOffset = 0.0f,
    float SlopeMultiplier = 1.0f,
    float SlopeMax = 1.0f,
    float SlopeMin = -1.0f
);