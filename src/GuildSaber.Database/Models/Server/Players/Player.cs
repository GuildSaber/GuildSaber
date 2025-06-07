using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.Server.Guilds.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Players;

public class Player
{
    public PlayerId Id { get; init; }
    public required PlayerInfo Info { get; set; }
    public required PlayerHardwareInfo HardwareInfo { get; set; }
    public required PlayerLinkedAccounts LinkedAccounts { get; set; }
    public required PlayerSubscriptionInfo SubscriptionInfo { get; set; }
    public bool IsManager { get; set; }

    public IList<Member> Members { get; init; } = null!;

    public readonly record struct PlayerId(uint Value) : IEFStrongTypedId<PlayerId, uint>
    {
        public static bool TryParse(string from, out PlayerId value)
        {
            if (uint.TryParse(from, out var id))
            {
                value = new PlayerId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator uint(PlayerId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGenericConversion<Player.PlayerId, uint>()
            .ValueGeneratedOnAdd();
        builder.ComplexProperty(x => x.Info);
        builder.ComplexProperty(x => x.HardwareInfo);
        builder.ComplexProperty(x => x.LinkedAccounts).Configure(new PlayerLinkedAccountsConfiguration());
        builder.ComplexProperty(x => x.SubscriptionInfo);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Player)
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}