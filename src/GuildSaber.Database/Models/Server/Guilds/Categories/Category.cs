using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Categories;

public class Category
{
    public CategoryId Id { get; init; }
    public Guild.GuildId GuildId { get; init; }
    public CategoryInfo Info { get; init; }

    public readonly record struct CategoryId(uint Value) : IEFStrongTypedId<CategoryId, uint>
    {
        public static bool TryParse(string from, out CategoryId value)
        {
            if (uint.TryParse(from, out var id))
            {
                value = new CategoryId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator uint(CategoryId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasGenericConversion<Category.CategoryId, uint>()
            .ValueGeneratedOnAdd();
        builder.ComplexProperty(x => x.Info).Configure(new CategoryInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.Categories).HasForeignKey(x => x.GuildId);
    }
}