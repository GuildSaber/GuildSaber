using CSharpFunctionalExtensions;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Categories;

public readonly record struct CategoryInfo(Name_2_50 Name, Description Description)
{
    public static Result<CategoryInfo> TryCreate(string name, string? description)
        => (name: Name_2_50.TryCreate(name), description: Description.TryCreate(description)) switch
        {
            { name: { IsFailure: true, Error: var error } } => Failure<CategoryInfo>(error),
            { description: { IsFailure: true, Error: var error } } => Failure<CategoryInfo>(error),
            var x => Success(new CategoryInfo(x.name.Value, x.description.Value))
        };
}

public class CategoryInfoConfiguration : IComplexPropertyConfiguration<CategoryInfo>
{
    public ComplexPropertyBuilder<CategoryInfo> Configure(ComplexPropertyBuilder<CategoryInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Name_2_50.MaxLength)
            .HasConversion<string>(from => from, to => Name_2_50.CreateUnsafe(to).Value);
        builder.Property(x => x.Description).HasMaxLength(Description.MaxLength)
            .HasConversion<string>(from => from, to => Description.CreateUnsafe(to).Value);

        return builder;
    }
}