using GuildSaber.Database.Models.Server.Guilds.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

/// <summary>
/// It's a level that overrides a base level for a specific category.
/// </summary>
public sealed class CategoryLevelOverride : Level
{
    public Category.CategoryId CategoryId { get; init; }
    public LevelId BaseLevelId { get; init; }
}

public class CategoryLevelOverrideConfiguration : IEntityTypeConfiguration<CategoryLevelOverride>
{
    public void Configure(EntityTypeBuilder<CategoryLevelOverride> builder)
    {
        builder.HasBaseType<Level>();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId);
        builder.HasOne<Level>()
            .WithMany()
            .HasForeignKey(x => x.BaseLevelId);
    }
}