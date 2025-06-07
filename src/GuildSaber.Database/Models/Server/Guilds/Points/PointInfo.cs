using CSharpFunctionalExtensions;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds.Points;

public readonly record struct PointInfo
{
    private PointInfo(Name_2_6 name, Description description)
        => (Name, Description) = (name, description);

    public Name_2_6 Name { get; init; }
    public Description Description { get; init; }

    public static Result<PointInfo> TryCreate(string name, string description)
        => (name: Name_2_6.TryCreate(name), description: Description.TryCreate(description)) switch
        {
            { name: { IsFailure: true, Error: var error } } => Failure<PointInfo>(error),
            { description: { IsFailure: true, Error: var error } } => Failure<PointInfo>(error),
            var x => Success(new PointInfo(x.name.Value, x.description.Value))
        };
}

public class PointInfoConfiguration : IComplexPropertyConfiguration<PointInfo>
{
    public ComplexPropertyBuilder<PointInfo> Configure(ComplexPropertyBuilder<PointInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Name_2_6.MaxLength)
            .HasConversion<string>(from => from, to => Name_2_6.CreateUnsafe(to).Value);
        builder.Property(x => x.Description).HasMaxLength(Description.MaxLength)
            .HasConversion<string>(from => from, to => Description.CreateUnsafe(to).Value);

        return builder;
    }
}