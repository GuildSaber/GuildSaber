using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.MapRequests;

/// <summary>A player's current boost allocation. Moving it changes its target. Releasing it deletes it.</summary>
public sealed class MapRequestThreadBoost
{
    public PlayerId PlayerId { get; init; }
    public MapRequestThreadId ThreadId { get; init; }

    public Player Player { get; init; } = null!;
    public MapRequestThread Thread { get; init; } = null!;
}

public class MapRequestThreadBoostConfiguration : IEntityTypeConfiguration<MapRequestThreadBoost>
{
    public void Configure(EntityTypeBuilder<MapRequestThreadBoost> builder)
    {
        builder.HasKey(x => new { x.PlayerId, x.ThreadId });

        builder.HasOne(x => x.Player)
            .WithMany(x => x.RequestThreadBoosts)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Thread)
            .WithMany(x => x.Boosts)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}