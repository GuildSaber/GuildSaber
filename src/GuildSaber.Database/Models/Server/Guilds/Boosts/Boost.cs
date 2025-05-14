using GuildSaber.Database.Models.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Boosts;

public class Boost
{
    public Player.PlayerId PlayerId { get; init; }
    public Guild.GuildId GuildId { get; init; }

    public EBoostType Type { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    public Player Player { get; init; } = null!;
    public Guild Guild { get; init; } = null!;

    [Flags]
    public enum EBoostType
    {
        Default = 0,
        Tier1 = 1 << 0,
        Tier2 = 1 << 1,
        Tier3 = 1 << 2
    }
}

public class BoostConfiguration : IEntityTypeConfiguration<Boost>
{
    public void Configure(EntityTypeBuilder<Boost> builder)
    {
        builder.HasKey(x => new { x.GuildId, x.PlayerId });
        builder.HasOne(x => x.Guild)
            .WithMany(x => x.Boosts)
            .HasForeignKey(x => x.GuildId);

        builder.HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId);
    }
}