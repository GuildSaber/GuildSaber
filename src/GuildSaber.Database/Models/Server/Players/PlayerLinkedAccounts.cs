using System.Diagnostics;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Players;

public readonly record struct PlayerLinkedAccounts(
    SteamId? SteamId,
    MetaPCId? MetaPCId,
    BLNativeId? BLNativeId,
    ScoreSaberId? ScoreSaberId,
    DiscordId? DiscordId
)
{
    public BeatLeaderId BeatLeaderId()
    {
        if (SteamId is not null) return SteamId.Value;
        if (MetaPCId is not null) return MetaPCId.Value;
        return BLNativeId ?? throw new UnreachableException();
    }
}

public class PlayerLinkedAccountsConfiguration : IComplexPropertyConfiguration<PlayerLinkedAccounts>
{
    public ComplexPropertyBuilder<PlayerLinkedAccounts> Configure(ComplexPropertyBuilder<PlayerLinkedAccounts> builder)
    {
        builder.Property(x => x.SteamId).HasConversion<ulong?>(from => from, to => SteamId.CreateUnsafe(to));
        builder.Property(x => x.MetaPCId).HasConversion<ulong?>(from => from, to => MetaPCId.CreateUnsafe(to));
        builder.Property(x => x.BLNativeId).HasConversion<ulong?>(from => from, to => BLNativeId.CreateUnsafe(to));
        builder.Property(x => x.ScoreSaberId).HasConversion<ulong?>(from => from, to => ScoreSaberId.CreateUnsafe(to));
        builder.Property(x => x.DiscordId).HasConversion<ulong?>(from => from, to => DiscordId.CreateUnsafe(to));

        return builder;
    }
}