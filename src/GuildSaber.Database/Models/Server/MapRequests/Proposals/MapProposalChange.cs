using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals;

public closed class MapProposalChange
{
    public MapProposalId ProposalId { get; init; }
    public MapProposal Proposal { get; init; } = null!;

    public sealed class Rank : MapProposalChange
    {
        public required RankedMapRequirements Requirements { get; init; }
        public required MapProposalRating Rating { get; init; }

        public IList<MapProposalVersion> MapVersions { get; init; } = null!;
        public IList<MapProposalCategory> Categories { get; init; } = null!;
        public IList<MapProposalAchievement> Achievements { get; init; } = null!;
    }

    public sealed class Unrank : MapProposalChange
    {
        public required MapProposalId TargetAcceptedProposalId { get; init; }
        public required string Reason { get; init; }
        public AcceptedProposal TargetAcceptedProposal { get; init; } = null!;
    }
}

public enum MapProposalChangeKind : byte
{
    Rank,
    Unrank
}

public sealed class MapProposalChangeConfiguration : IEntityTypeConfiguration<MapProposalChange>
{
    public void Configure(EntityTypeBuilder<MapProposalChange> builder)
    {
        builder.HasKey(x => x.ProposalId);
        builder.Property(x => x.ProposalId).ValueGeneratedNever();

        builder.HasDiscriminator<MapProposalChangeKind>("Kind")
            .HasValue<MapProposalChange.Rank>(MapProposalChangeKind.Rank)
            .HasValue<MapProposalChange.Unrank>(MapProposalChangeKind.Unrank)
            .IsComplete();

        builder.HasOne(x => x.Proposal).WithOne(x => x.Change)
            .HasForeignKey<MapProposalChange>(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RankProposalChangeConfiguration : IEntityTypeConfiguration<MapProposalChange.Rank>
{
    public void Configure(EntityTypeBuilder<MapProposalChange.Rank> builder)
    {
        builder.HasBaseType<MapProposalChange>();
        builder.ComplexProperty(x => x.Requirements).Configure(new RankedMapRequirementsConfiguration());
        builder.ComplexProperty(x => x.Rating).Configure(new MapProposalRatingConfiguration());
    }
}

public sealed class UnrankProposalChangeConfiguration : IEntityTypeConfiguration<MapProposalChange.Unrank>
{
    public void Configure(EntityTypeBuilder<MapProposalChange.Unrank> builder)
    {
        builder.HasBaseType<MapProposalChange>();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.HasOne(x => x.TargetAcceptedProposal).WithMany()
            .HasForeignKey(x => x.TargetAcceptedProposalId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}