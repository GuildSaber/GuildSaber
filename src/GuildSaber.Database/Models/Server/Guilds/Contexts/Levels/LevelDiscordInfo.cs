using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public record struct LevelDiscordInfo(DiscordRoleId? RoleId);

public class LevelDiscordInfoConfiguration : IComplexPropertyConfiguration<LevelDiscordInfo>
{
    public ComplexPropertyBuilder<LevelDiscordInfo> Configure(ComplexPropertyBuilder<LevelDiscordInfo> builder)
    {
        builder.Property(x => x.RoleId)
            .HasConversion<ulong?>(from => from, to => DiscordRoleId.CreateUnsafe(to))
            .HasColumnType("numeric(20,0)");

        return builder;
    }
}