using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;

public sealed class SubmittedProposal : MapProposal
{
    public required DateTimeOffset SubmittedAt { get; init; }
}

public class SubmittedProposalConfiguration : IEntityTypeConfiguration<SubmittedProposal>
{
    public void Configure(EntityTypeBuilder<SubmittedProposal> builder)
    {
        builder.HasBaseType<MapProposal>();

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("SubmittedAt");
    }
}
