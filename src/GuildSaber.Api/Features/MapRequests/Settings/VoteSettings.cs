using System.ComponentModel.DataAnnotations;

namespace GuildSaber.Api.Features.MapRequests.Settings;

/// <summary>Vote requirements for explicit acceptance by the ranking team, bypassed by leaders and managers.</summary>
public sealed class MapProposalReviewSettings
{
    public const string SectionKey = "MapProposalReviewSettings";

    [Range(0, int.MaxValue)] public int MinimumPositiveVotes { get; init; } = 3;
    public bool RequireNoNegativeVotes { get; init; } = true;
}