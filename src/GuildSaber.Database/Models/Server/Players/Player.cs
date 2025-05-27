using GuildSaber.Database.Models.Server.StrongTypes.Abstractions;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Players;

public class Player
{
    public PlayerId Id { get; init; }
    public PlayerInfo Info { get; set; }
    public PlayerHardwareInfo HardwareInfo { get; set; }
    public PlayerLinkedAccounts LinkedAccounts { get; set; }
    public PlayerSubscriptionInfo SubscriptionInfo { get; set; }

    public readonly record struct PlayerId(ulong Value) : IStrongType<ulong>
    {
        public static bool TryParse(string from, out PlayerId value)
        {
            if (ulong.TryParse(from, out var id))
            {
                value = new PlayerId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator ulong(PlayerId id)
            => id.Value;
    }
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