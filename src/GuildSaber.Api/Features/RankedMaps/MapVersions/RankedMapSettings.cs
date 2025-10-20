using System.ComponentModel.DataAnnotations;
using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Api.Features.RankedMaps.MapVersions;

public class RankedMapSettings
{
    public const string RankedMapSettingsSectionKey = "RankedMapSettings";

    [Required] public required GuildCreationSettings Creation { get; init; }
}

public class GuildCreationSettings
{
    [Required] public PlayerSubscriptionInfo.ESubscriptionTier RequiredSubscriptionTier { get; init; }
    [Required, Range(0, 5)] public int MaxGuildCountPerUser { get; init; }
}