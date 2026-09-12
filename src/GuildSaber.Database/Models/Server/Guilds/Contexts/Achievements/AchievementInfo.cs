using System.Drawing;
using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Achievements;

public record struct AchievementInfo(
    string Name,
    Color Color
);

public class AchievementInfoConfiguration : IComplexPropertyConfiguration<AchievementInfo>
{
    public ComplexPropertyBuilder<AchievementInfo> Configure(ComplexPropertyBuilder<AchievementInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(32);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));

        return builder;
    }
}