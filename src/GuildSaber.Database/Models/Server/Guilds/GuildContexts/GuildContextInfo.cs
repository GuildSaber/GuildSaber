using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildContextInfo(
    string Name,
    string Description
);

public class GuildContextInfoConfiguration : IComplexPropertyConfiguration<GuildContextInfo>
{
    public ComplexPropertyBuilder<GuildContextInfo> Configure(ComplexPropertyBuilder<GuildContextInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(128);
        return builder;
    }
}