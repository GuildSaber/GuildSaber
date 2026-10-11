using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests.Votes;

/// <summary>A community vote on a thread, independent of votes on its individual proposals.</summary>
public sealed class MapRequestThreadVote
{
    public MapRequestThreadId ThreadId { get; init; }
    public PlayerId PlayerId { get; init; }

    public required bool IsPositive { get; set; }
    public required DateTimeOffset VotedAt { get; set; }

    public MapRequestThread Thread { get; init; } = null!;
    public Player Player { get; init; } = null!;
}

public class MapRequestThreadVoteConfiguration : IEntityTypeConfiguration<MapRequestThreadVote>
{
    public void Configure(EntityTypeBuilder<MapRequestThreadVote> builder)
    {
        builder.HasKey(x => new { x.ThreadId, x.PlayerId });

        builder.HasOne(x => x.Thread)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Player)
            .WithMany(x => x.RequestThreadVotes)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
