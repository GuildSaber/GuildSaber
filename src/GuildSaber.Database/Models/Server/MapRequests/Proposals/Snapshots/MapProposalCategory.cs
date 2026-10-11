using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;

public sealed class MapProposalCategory
{
    public MapProposalId ProposalId { get; init; }

    /// <summary>Historical identity, deliberately not a foreign key to the live category.</summary>
    public CategoryId CategoryId { get; init; }

    public required string Name { get; init; }

    public MapProposal Proposal { get; init; } = null!;
}

public class MapProposalCategoryConfiguration : IEntityTypeConfiguration<MapProposalCategory>
{
    public void Configure(EntityTypeBuilder<MapProposalCategory> builder)
    {
        builder.HasKey(x => new { x.ProposalId, x.CategoryId });
        builder.Property(x => x.CategoryId)
            .HasConversion(from => from.Value, to => new CategoryId(to));
        builder.Property(x => x.Name).HasMaxLength(50);

        builder.HasOne(x => x.Proposal)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
