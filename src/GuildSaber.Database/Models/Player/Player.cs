using GuildSaber.Database.Models.StrongTypes.Abstractions;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Player;

public class Player
{
    public PlayerId Id { get; init; }
    public PlayerInfo Info { get; set; }
    public PlayerHardwareInfo HardwareInfo { get; set; }
    public PlayerLinkedAccounts LinkedAccounts { get; set; }
    public PlayerSubscriptionInfo SubscriptionInfo { get; set; }

    public readonly record struct PlayerId(ulong Value) : IStrongType<ulong>;
}

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<Player.PlayerId, ulong>();
        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.HardwareInfo);
        builder.ComplexProperty(x => x.LinkedAccounts).Configure(new PlayerLinkedAccountsConfiguration());
        builder.ComplexProperty(x => x.SubscriptionInfo);
    }
}