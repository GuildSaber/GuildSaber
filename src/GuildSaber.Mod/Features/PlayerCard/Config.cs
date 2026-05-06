using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.Common.Timer;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard;

public class CardConfig
{
    public CardColors ColorSettings = new(false, false, Color.white, Color.white, Color.white);

    public TimerConfig TimerConfig = new();

    public CardTransforms Transforms = new(
        Menu: new CardTransform(
            new Vector3(0.0f, 0.02f, 1.0f),
            Quaternion.Euler(90.0f, 0.0f, 0.0f)
        ),
        InSong: new CardTransform(
            new Vector3(-3.0f, 0.8f, 0),
            Quaternion.Euler(20, 270, 0)
        )
    );

    public bool CategoryLevelViewEnabled { get; set; } = true;

    public GuildId GuildId { get; set; } = new(-1);
    public ContextId ContextId { get; set; } = new(-1);
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