using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

/// <summary>
/// It's a level that applies globally, regardless of category (unless stated otherwise).
/// </summary>
public sealed class GlobalLevel : Level
{
    public bool IsIgnoredInCategories { get; set; }
}

public class GlobalLevelConfiguration : IEntityTypeConfiguration<GlobalLevel>
{
    public void Configure(EntityTypeBuilder<GlobalLevel> builder) => builder.HasBaseType<Level>();
}