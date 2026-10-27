using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Features.MapRequests.Settings;

/// <summary>Per-player, per-context proposal limits. Boost allowances are configured separately and are global.</summary>
public sealed class MapRequestSettings
{
    public const string SectionKey = "MapRequestSettings";
    [Range(0, int.MaxValue)] public int MaxDraftsPerPlayer { get; init; } = 10;
    [Range(0, int.MaxValue)] public int MaxRankingRequestsPerPlayer { get; init; } = 5;
    [Range(0, int.MaxValue)] public int MaxRevisionRequestsPerPlayer { get; init; } = 10;

    /// <summary>Minimum subscription tier for proposing a competing variant. 0 allows all players.</summary>
    [Range(0, 3)] public int MinimumAmendmentTier { get; init; }

    /// <summary>Each boost multiplies waiting-time priority by this additional fraction.</summary>
    [Range(0, 100)] public double PriorityBonusPerBoost { get; init; } = 0.25;
}