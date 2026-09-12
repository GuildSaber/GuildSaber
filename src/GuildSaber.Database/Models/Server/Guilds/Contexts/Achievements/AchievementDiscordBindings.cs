using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Achievements;

public record struct AchievementDiscordBindings(DiscordRoleId? RoleId);

public class AchievementDiscordBindingsConfiguration : IComplexPropertyConfiguration<AchievementDiscordBindings>
{
    public ComplexPropertyBuilder<AchievementDiscordBindings> Configure(
        ComplexPropertyBuilder<AchievementDiscordBindings> builder)
    {
        builder.Property(x => x.RoleId)
            .HasConversion<ulong?>(from => from, to => DiscordRoleId.CreateUnsafe(to))
            .HasColumnType("numeric(20,0)");

        return builder;
    }
}