using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Players;

public readonly record struct PlayerLinkedAccounts(
    BLId BeatLeaderId,
    ScoreSaberId? ScoreSaberId,
    DiscordId? DiscordId
);

public class PlayerLinkedAccountsConfiguration : IComplexPropertyConfiguration<PlayerLinkedAccounts>
{
    public ComplexPropertyBuilder<PlayerLinkedAccounts> Configure(ComplexPropertyBuilder<PlayerLinkedAccounts> builder)
    {
        builder.Property(x => x.BeatLeaderId).HasConversion<ulong>(from => from, to => BLId.CreateUnsafe(to).Value);
        builder.Property(x => x.ScoreSaberId).HasConversion<ulong?>(from => from, to => ScoreSaberId.CreateUnsafe(to));
        builder.Property(x => x.DiscordId).HasConversion<ulong?>(from => from, to => DiscordId.CreateUnsafe(to));

        return builder;
    }
}