using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlayModeId = GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes.PlayMode.PlayModeId;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;

namespace GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;

public sealed class MapProposalVersion
{
    public MapProposalId ProposalId { get; init; }
    public SongDifficultyId SongDifficultyId { get; init; }

    /// <summary>Historical identity, deliberately not a foreign key to the live play mode.</summary>
    public PlayModeId PlayModeId { get; init; }

    public required string PlayModeName { get; init; }
    public byte Order { get; init; }

    public MapProposal Proposal { get; init; } = null!;
    public SongDifficulty SongDifficulty { get; init; } = null!;
}

public class MapProposalVersionConfiguration : IEntityTypeConfiguration<MapProposalVersion>
{
    public void Configure(EntityTypeBuilder<MapProposalVersion> builder)
    {
        builder.HasKey(x => new { x.ProposalId, x.SongDifficultyId, x.PlayModeId });
        builder.Property(x => x.PlayModeId).HasGenericConversion<PlayModeId, int>();
        builder.Property(x => x.PlayModeName).HasMaxLength(128);

        builder.HasOne(x => x.Proposal)
            .WithMany(x => x.MapVersions)
            .HasForeignKey(x => x.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SongDifficulty)
            .WithMany()
            .HasForeignKey(x => x.SongDifficultyId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}