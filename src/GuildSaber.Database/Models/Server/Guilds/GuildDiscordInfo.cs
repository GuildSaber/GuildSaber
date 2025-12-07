using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildDiscordInfo(DiscordGuildId? MainDiscordGuildId);

public class GuildDiscordInfoConfiguration : IComplexPropertyConfiguration<GuildDiscordInfo>
{
    public ComplexPropertyBuilder<GuildDiscordInfo> Configure(ComplexPropertyBuilder<GuildDiscordInfo> builder)
    {
        builder.Property(x => x.MainDiscordGuildId)
            .HasConversion<ulong?>(from => from, to => DiscordGuildId.CreateUnsafe(to))
            .HasColumnType("numeric(20,0)");

        return builder;
    }
}