using System.Runtime.CompilerServices;
using GuildSaber.Mod.Features.PlayerCard;
using GuildSaber.Mod.Features.RankedMapStats;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace GuildSaber.Mod.Features.GuildSaber;

public class GuildSaberConfig
{
    public bool Enabled { get; init; } = true;
    public ApiEnv ApiEnv { get; set; } = ApiEnv.Dev;

    public CardConfig PlayerCard { get; init; } = new();
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