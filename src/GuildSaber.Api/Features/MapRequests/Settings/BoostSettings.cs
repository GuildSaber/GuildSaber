using System.ComponentModel.DataAnnotations;
using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Api.Features.MapRequests.Settings;

/// <summary>Maximum simultaneous thread boosts per player, shared across all guilds and contexts.</summary>
public sealed class MapRequestBoostSettings
{
    public const string SectionKey = "MapRequestBoostSettings";

    [Range(0, int.MaxValue)] public int None { get; init; } = 1;
    [Range(0, int.MaxValue)] public int Tier1 { get; init; } = 5;
    [Range(0, int.MaxValue)] public int Tier2 { get; init; } = 10;
    [Range(0, int.MaxValue)] public int Tier3 { get; init; } = 15;

    public int GetLimit(PlayerSubscriptionInfo.ESubscriptionTier tier) => tier switch
    {
        PlayerSubscriptionInfo.ESubscriptionTier.None => None,
        PlayerSubscriptionInfo.ESubscriptionTier.Tier1 => Tier1,
        PlayerSubscriptionInfo.ESubscriptionTier.Tier2 => Tier2,
        PlayerSubscriptionInfo.ESubscriptionTier.Tier3 => Tier3,
        _ => 0
    };
}