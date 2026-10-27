using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AchievementId = GuildSaber.Database.Models.Server.Guilds.Achievements.Achievement.AchievementId;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;

/// <summary>
/// A proposed explicit ranked-map-list achievement association. Star achievements are matched dynamically.
/// </summary>
public sealed class MapProposalAchievement
{
    public MapProposalId ProposalId { get; init; }

    /// <summary>Historical identity, deliberately not a foreign key to the live achievement.</summary>
    public AchievementId AchievementId { get; init; }

    public required string Name { get; init; }

    public MapProposal Proposal { get; init; } = null!;
}

public class MapProposalAchievementConfiguration : IEntityTypeConfiguration<MapProposalAchievement>
{
    public void Configure(EntityTypeBuilder<MapProposalAchievement> builder)
    {
        builder.HasKey(x => new { x.ProposalId, x.AchievementId });
        builder.Property(x => x.AchievementId).HasGenericConversion<AchievementId, int>();
        builder.Property(x => x.Name).HasMaxLength(32);

        builder.HasOne(x => x.Proposal)
            .WithMany(x => x.Achievements)
            .HasForeignKey(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}