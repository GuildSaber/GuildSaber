using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Features.RankedMaps;

public class RankedMapSettings
{
    public const string RankedMapSettingsSectionKey = "RankedMapSettings";

    [Required] public required RankedMapDefaultSettings DefaultSettings { get; init; }
    [Required] public required RankedMapBoostSettings BoostSettings { get; init; }
}

public class RankedMapDefaultSettings
{
    [Required, Range(0, int.MaxValue)]
    public int MaxRankedMapCount { get; init; }
}

public class RankedMapBoostSettings
{
    [Required] public RankedMapCountBoostValues MapCountBoosts { get; init; }
    public readonly record struct RankedMapCountBoostValues(int Tier1, int Tier2, int Tier3);
}