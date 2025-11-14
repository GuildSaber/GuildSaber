using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct ContextInfo(
    string Name,
    string Description
);

public class ContextInfoConfiguration : IComplexPropertyConfiguration<ContextInfo>
{
    public ComplexPropertyBuilder<ContextInfo> Configure(ComplexPropertyBuilder<ContextInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(128);
        return builder;
    }
}