using System;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard;

public class PlayerCardConfig
{
    public CardColors ColorSettings = new(false, false, Color.white, Color.white, Color.white);
    public PlayerCardPlayTimeConfig TimerConfig = new();

    public CardTransforms Transforms = new(
        Menu: new CardTransform(
            new Vector3(0.0f, 0.02f, 1.0f),
            Quaternion.Euler(90.0f, 0.0f, 0.0f)
        ),
        InSong: new CardTransform(
            new Vector3(-2.8f, 0.45f, 0),
            Quaternion.Euler(20, 270, 0)
        )
    );

    public bool Enabled { get; set; } = true;
    public int SchemaVersion { get; set; }

    public PlayerCardColorMode ColorMode { get; set; } = PlayerCardColorMode.Automatic;
    /// <summary>Shows category ordered achievements (aka. levels), their average, and skill equilibrium.</summary>
    public bool ShowOrderedAchievements { get; set; } = true;
    public bool ShowHandle { get; set; }

    public void Migrate()
    {
        switch (SchemaVersion)
        {
            case 0:
                SetColorMode(ColorSettings switch
                {
                    { UseCustomColors: false } => PlayerCardColorMode.Automatic,
                    { UseGradient: true } => PlayerCardColorMode.Gradient,
                    _ => PlayerCardColorMode.Solid
                });
                SchemaVersion = 1;
                break;
        }
    }

    public void SetColorMode(PlayerCardColorMode mode)
    {
        ColorMode = Enum.IsDefined(typeof(PlayerCardColorMode), mode)
            ? mode
            : PlayerCardColorMode.Automatic;
        ColorSettings.UseCustomColors = ColorMode is not PlayerCardColorMode.Automatic;
        ColorSettings.UseGradient = ColorMode is PlayerCardColorMode.Gradient;
    }
}

/// <summary>
/// Card positions and rotations depending on the context (in menu or in song)
/// </summary>
public record struct CardTransforms(
    CardTransform Menu,
    CardTransform InSong
);

/// <summary>
/// Card position and rotation
/// </summary>
public record struct CardTransform(
    Vector3 Position,
    Quaternion Rotation
);

public record struct CardColors(
    bool UseCustomColors,
    bool UseGradient,
    Color MainCardColor,
    Color GradientColor0,
    Color GradientColor1
);

public enum PlayerCardColorMode
{
    Automatic,
    Solid,
    Gradient
}

public class PlayerCardPlayTimeConfig
{
    public long PlayDurationSec { get; set; }
    public int Day { get; set; } = -1;
}
