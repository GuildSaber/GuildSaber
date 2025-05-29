using System.Drawing;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildInfo
{
    private GuildInfo(Name name, SmallName smallName, Description description, Color color, DateTimeOffset createdAt)
        => (Name, SmallName, Description, Color, CreatedAt) = (name, smallName, description, color, createdAt);

    public Name Name { get; init; }
    public SmallName SmallName { get; init; }
    public Description Description { get; init; }
    public Color Color { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static Result<GuildInfo> TryCreate
        (string name, string smallName, string description, Color color, DateTimeOffset createdAt)
        => (name: Name.TryCreate(name), smallName: SmallName.TryCreate(smallName),
                description: Description.TryCreate(description), color, createdAt) switch
            {
                { name: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { smallName: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { description: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { color.IsEmpty: true } => Failure<GuildInfo>("GuildInfo.Color must not be empty"),
                { createdAt.UtcTicks: 0 } => Failure<GuildInfo>("GuildInfo.CreatedAt must not be 0"),
                var x => Success(
                    new GuildInfo(x.name.Value, x.smallName.Value, x.description.Value, x.color, x.createdAt)
                )
            };
}

public class GuildInfoConfiguration : IComplexPropertyConfiguration<GuildInfo>
{
    public ComplexPropertyBuilder<GuildInfo> Configure(ComplexPropertyBuilder<GuildInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Name.MaxLength)
            .HasConversion<string>(from => from, to => Name.CreateUnsafe(to).Value);
        builder.Property(x => x.SmallName).HasMaxLength(SmallName.MaxLength)
            .HasConversion<string>(from => from, to => SmallName.CreateUnsafe(to).Value);
        builder.Property(x => x.Description).HasMaxLength(Description.MaxLength)
            .HasConversion<string>(from => from, to => Description.CreateUnsafe(to).Value);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));

        return builder;
    }
}