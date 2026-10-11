using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Votes;

/// <summary>A community or ranking-team vote on a proposal, not an acceptance decision.</summary>
public closed class MapProposalVote
{
    public MapProposalId ProposalId { get; init; }
    public PlayerId PlayerId { get; init; }

    public required bool IsPositive { get; set; }
    public required DateTimeOffset VotedAt { get; set; }

    public MapProposal Proposal { get; init; } = null!;
    public Player Player { get; init; } = null!;
}

public class MapProposalVoteConfiguration : IEntityTypeConfiguration<MapProposalVote>
{
    public void Configure(EntityTypeBuilder<MapProposalVote> builder)
    {
        builder.HasDiscriminator<byte>("Kind")
            .HasValue<CommunityProposalVote>(0)
            .HasValue<RankingTeamProposalVote>(1)
            .IsComplete();

        builder.HasKey(nameof(MapProposalVote.ProposalId), nameof(MapProposalVote.PlayerId), "Kind");

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_MapProposalVotes_Kind",
            "(\"Kind\" = 0 AND \"Reason\" IS NULL) OR \"Kind\" = 1"));

        builder.HasOne(x => x.Proposal)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Player)
            .WithMany(x => x.ProposalVotes)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
