using System.Diagnostics;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
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
        // Can't setup indexes on complex type yet: https://github.com/dotnet/efcore/issues/31246, work seems in progress though.
        // builder.HasIndex(x => x.SteamId); builder.HasIndex(x => x.MetaPCId); builder.HasIndex(x => x.BLNativeId); builder.HasIndex(x => x.DiscordId);

        // The BeatLeaderId is an expression that will be translated to SQL, we should't have EFCore map it to a column.
        //builder.Ignore(x => x.BeatLeaderId);
        builder.Property(x => x.SteamId).HasConversion<ulong?>(from => from, to => SteamId.CreateUnsafe(to));
        builder.Property(x => x.MetaPCId).HasConversion<ulong?>(from => from, to => MetaPCId.CreateUnsafe(to));
        builder.Property(x => x.BLNativeId).HasConversion<ulong?>(from => from, to => BLNativeId.CreateUnsafe(to));
        builder.Property(x => x.ScoreSaberId).HasConversion<ulong?>(from => from, to => ScoreSaberId.CreateUnsafe(to));
        builder.Property(x => x.DiscordId).HasConversion<ulong?>(from => from, to => DiscordId.CreateUnsafe(to));


        return builder;
    }
}