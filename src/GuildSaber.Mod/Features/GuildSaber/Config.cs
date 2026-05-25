using System.Runtime.CompilerServices;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Mod.Features.MenuTweaks.RankedMapStats;
using GuildSaber.Mod.Features.PlayerCard;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace GuildSaber.Mod.Features.GuildSaber;

public class GuildSaberConfig
{
    public bool Enabled { get; init; } = true;
    public ApiEnv ApiEnv { get; set; } = ApiEnv.Dev;

    public GuildId GuildId { get; set; } = new(-1);
    public ContextId ContextId { get; set; } = new(-1);

    public PlayerCardConfig PlayerCard { get; init; } = new();
    public RankedMapStatsConfig RankedMapStats { get; init; } = new();
}

/// <summary>
/// API Environment
/// </summary>
/// <remarks>Order must be maintained as it's used for dropdown index</remarks>
public enum ApiEnv
{
    Prod,
    Dev
}