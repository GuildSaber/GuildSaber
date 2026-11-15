using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals;

/// <summary>A requested ranking change and its review lifecycle.</summary>
public closed class MapProposal
{
    public MapProposalId Id { get; init; }
    public MapRequestThreadId ThreadId { get; init; }
    public PlayerId? AuthorId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public string? Comment { get; init; }

    public required MapProposalChange Change { get; init; }

    public MapRequestThread Thread { get; init; } = null!;
    public Player? Author { get; init; }
    public IList<MapProposalVote> Votes { get; init; } = null!;
}

public enum MapProposalStatus : byte
{
    Draft = 0,
    Submitted = 1,
    Accepted = 2,
    Closed = 3
}

public class MapProposalConfiguration : IEntityTypeConfiguration<MapProposal>
{
    public void Configure(EntityTypeBuilder<MapProposal> builder)
    {
        builder.HasDiscriminator<MapProposalStatus>("status")
            .HasValue<DraftProposal>(MapProposalStatus.Draft)
            .HasValue<SubmittedProposal>(MapProposalStatus.Submitted)
            .HasValue<AcceptedProposal>(MapProposalStatus.Accepted)
            .HasValue<ClosedProposal>(MapProposalStatus.Closed)
            .IsComplete();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new MapProposalId(to))
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Comment).HasMaxLength(2000);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_MapProposals_Lifecycle",
                $"""
                 ("status" = {(byte)MapProposalStatus.Draft}
                     AND "SubmittedAt" IS NULL AND "AcceptedAt" IS NULL AND "ClosedAt" IS NULL)
                 OR ("status" = {(byte)MapProposalStatus.Submitted}
                     AND "SubmittedAt" IS NOT NULL AND "AcceptedAt" IS NULL AND "ClosedAt" IS NULL)
                 OR ("status" = {(byte)MapProposalStatus.Accepted}
                     AND "SubmittedAt" IS NOT NULL AND "AcceptedAt" IS NOT NULL AND "ClosedAt" IS NULL)
                 OR ("status" = {(byte)MapProposalStatus.Closed}
                     AND "AcceptedAt" IS NULL AND "ClosedAt" IS NOT NULL)
                 """);
        });

        builder.HasOne(x => x.Thread)
            .WithMany(x => x.Proposals)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}