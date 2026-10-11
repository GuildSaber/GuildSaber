namespace GuildSaber.Database.Models.Server.MapRequests.Votes;

public sealed class RankingTeamProposalVote : MapProposalVote
{
    /// <summary>An optional explanation for the positive or negative review vote.</summary>
    public string? Reason { get; set; }
}
