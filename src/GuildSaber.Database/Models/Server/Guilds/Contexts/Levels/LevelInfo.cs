using System.Drawing;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

public record struct LevelInfo(
    string Name,
    Color Color
);

public class LevelInfoConfiguration : IComplexPropertyConfiguration<LevelInfo>
{
    public ComplexPropertyBuilder<LevelInfo> Configure(ComplexPropertyBuilder<LevelInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(32);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));

        return builder;
    }
}