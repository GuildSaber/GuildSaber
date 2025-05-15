namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct CurveSettings(
    float Slope,
    float SlopeOffset,
    float SlopeMultiplier,
    float SlopeMax,
    float SlopeMin
)
{
    public static readonly CurveSettings Default = new()
    {
        Slope = 0.0f,
        SlopeOffset = 0.0f,
        SlopeMultiplier = 1.0f,
        SlopeMax = 1.0f,
        SlopeMin = -1.0f
    };
}