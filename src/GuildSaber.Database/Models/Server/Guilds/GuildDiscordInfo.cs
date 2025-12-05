using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildDiscordInfo(ulong? MainDiscordGuildId);

public class GuildDiscordInfoConfiguration : IComplexPropertyConfiguration<GuildDiscordInfo>
{
    public ComplexPropertyBuilder<GuildDiscordInfo> Configure(ComplexPropertyBuilder<GuildDiscordInfo> builder)
    {
        builder.Property(x => x.MainDiscordGuildId)
            .HasColumnType("numeric(20,0)");

        return builder;
    }
}