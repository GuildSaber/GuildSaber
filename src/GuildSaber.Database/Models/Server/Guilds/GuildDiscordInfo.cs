using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildDiscordInfo(DiscordGuildId? MainDiscordGuildId, string? DiscordInviteCode);

public class GuildDiscordInfoConfiguration : IComplexPropertyConfiguration<GuildDiscordInfo>
{
    public ComplexPropertyBuilder<GuildDiscordInfo> Configure(ComplexPropertyBuilder<GuildDiscordInfo> builder)
    {
        builder.Property(x => x.MainDiscordGuildId)
            .HasConversion<ulong?>(from => from, to => DiscordGuildId.CreateUnsafe(to))
            .HasColumnType("numeric(20,0)");

        builder.Property(x => x.DiscordInviteCode)
            .HasMaxLength(32);

        return builder;
    }
}