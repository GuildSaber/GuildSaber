using GuildSaber.Database.Models.Server.Guilds.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Levels;

/// <summary>
/// It's a level that only applies to a specific category.
/// </summary>
public sealed class CategoryLevel : Level
{
    public Category.CategoryId CategoryId { get; init; }
}

public class CategoryLevelConfiguration : IEntityTypeConfiguration<CategoryLevel>
{
    public void Configure(EntityTypeBuilder<CategoryLevel> builder)
    {
        builder.HasBaseType<Level>();
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId);
    }
}