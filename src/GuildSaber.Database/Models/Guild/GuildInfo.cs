using System.Drawing;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guild;

public readonly record struct GuildInfo(
    string Description,
    Color Color,
    DateTimeOffset CreatedAt
);

public class GuildInfoConfiguration : IComplexPropertyConfiguration<GuildInfo>
{
    public ComplexPropertyBuilder<GuildInfo> Configure(ComplexPropertyBuilder<GuildInfo> builder)
    {
        builder.Property(x => x.Description).HasMaxLength(100);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));
       
        return builder;
    }
}