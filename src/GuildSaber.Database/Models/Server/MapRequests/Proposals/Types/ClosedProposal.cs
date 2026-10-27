using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;

public sealed class ClosedProposal : MapProposal
{
    public DateTimeOffset? SubmittedAt { get; init; }
    public required DateTimeOffset ClosedAt { get; init; }
    public PlayerId? ClosedById { get; init; }
    public string? Reason { get; init; }

    public Player? ClosedBy { get; init; }
}

public class ClosedProposalConfiguration : IEntityTypeConfiguration<ClosedProposal>
{
    public void Configure(EntityTypeBuilder<ClosedProposal> builder)
    {
        builder.HasBaseType<MapProposal>();

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("SubmittedAt");

        builder.HasOne(x => x.ClosedBy)
            .WithMany()
            .HasForeignKey(x => x.ClosedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}