using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace GuildSaber.Mod.Configurations;

internal class GSConfig
{
    public bool Enabled { get; init; } = true;
    public ApiEnv ApiEnv { get; init; } = ApiEnv.Dev;
    public CardConfig PlayerCard { get; init; } = new();

    /// <remarks>Useful when you need to find the original value of a parameter</remarks>
    public GSConfig Default() => new();
}

internal class CardConfig
{
    public bool CategoryLevelViewEnabled { get; set; } = true;

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

    public TimeConfig TimeData { get; set; } = new TimeConfig();
    
    public int GuildId { get; set; } = -1;
}

internal class TimeConfig
{
    public int PlayDurationSec { get; set; } = 0;
}

/// <summary>
/// Card positions and rotations depending on the context (in menu or in song)
/// </summary>
internal record struct CardTransforms(
    CardTransform Menu,
    CardTransform InSong
);

/// <summary>
/// Card position and rotation
/// </summary>
internal record struct CardTransform(
    Vector3 Position,
    Quaternion Rotation
);

/// <summary>
/// API Environment
/// </summary>
public enum ApiEnv
{
    Prod,
    Dev
}