using GuildSaber.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Categories;

public class Category
{
    public CategoryId Id { get; init; }
    public GuildId GuildId { get; init; }
    public CategoryInfo Info { get; set; }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(from => from.Value, to => new CategoryId(to))
            .ValueGeneratedOnAdd();

        builder.ComplexProperty(x => x.Info).Configure(new CategoryInfoConfiguration());

        builder.HasOne<Guild>()
            .WithMany(x => x.Categories).HasForeignKey(x => x.GuildId);
    }
}