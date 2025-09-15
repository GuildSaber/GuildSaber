namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct ModifierSettings(
    float OffPlatform = -0.50f,
    float NoFail = -0.50f,
    float NoBombs = -0.10f,
    float NoArrows = -0.50f,
    float NoObstacles = -0.20f,
    float SlowerSong = -0.30f,
    float FasterSong = +0.08f,
    float SuperFastSong = +0.36f,
    float GhostNotes = +0.04f,
    float DisappearingArrows = +0.00f,
    float BatteryEnergy = +0.00f,
    float InstaFail = +0.00f,
    float SmallNotes = +0.00f,
    float ProMode = +0.00f,
    float StrictAngles = +0.00f,
    float OldDots = +0.00f
);