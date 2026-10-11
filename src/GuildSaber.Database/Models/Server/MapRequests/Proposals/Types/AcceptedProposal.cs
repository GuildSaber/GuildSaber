using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;

public sealed class AcceptedProposal : MapProposal
{
    public required DateTimeOffset SubmittedAt { get; init; }
    public required DateTimeOffset AcceptedAt { get; init; }
    public PlayerId? AcceptedById { get; init; }

    public Player? AcceptedBy { get; init; }
}

public class AcceptedProposalConfiguration : IEntityTypeConfiguration<AcceptedProposal>
{
    public void Configure(EntityTypeBuilder<AcceptedProposal> builder)
    {
        builder.HasBaseType<MapProposal>();

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("SubmittedAt");

        builder.HasOne(x => x.AcceptedBy)
            .WithMany()
            .HasForeignKey(x => x.AcceptedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
