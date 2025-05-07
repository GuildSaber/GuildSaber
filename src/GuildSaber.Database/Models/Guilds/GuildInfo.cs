using System.Drawing;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Models.StrongTypes;
using GuildSaber.Database.Utils;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Guilds;

public readonly record struct GuildInfo
{
    public string Name { get; init; }
    public string SmallName { get; init; }
    public Description Description { get; init; }
    public Color Color { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    private GuildInfo(string name, string smallName, Description description, Color color, DateTimeOffset createdAt)
        => (Name, SmallName, Description, Color, CreatedAt) = (name, smallName, description, color, createdAt);

    public static Result<GuildInfo> TryCreate(string name, string smallName, string description, Color color,
                                              DateTimeOffset createdAt)
        => (name: name.Trim(), smallName: smallName.Trim(), description: Description.TryCreate(description), color,
                createdAt) switch
            {
                { name.Length: < 5 } => Failure<GuildInfo>("GuildInfo.Name must be at least 5 of length"),
                { name.Length: > 50 } => Failure<GuildInfo>("GuildInfo.Name must be at maximum 50 of length"),
                { smallName.Length: < 2 } => Failure<GuildInfo>("GuildInfo.SmallName must be at least 2 of length"),
                { smallName.Length: > 6 } => Failure<GuildInfo>("GuildInfo.SmallName must be at maximum 6 of length"),
                { description: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { color.IsEmpty: true } => Failure<GuildInfo>("GuildInfo.Color must not be empty"),
                { createdAt.UtcTicks: 0 } => Failure<GuildInfo>("GuildInfo.CreatedAt must not be 0"),
                var x => Success(new GuildInfo(x.name, x.smallName, x.description.Value, x.color, x.createdAt))
            };
}

public class GuildInfoConfiguration : IComplexPropertyConfiguration<GuildInfo>
{
    public ComplexPropertyBuilder<GuildInfo> Configure(ComplexPropertyBuilder<GuildInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50);
        builder.Property(x => x.SmallName).HasMaxLength(6);
        builder.Property(x => x.Description).HasMaxLength(Description.MaxLength)
            .HasConversion(from => from, to => Description.CreateUnsafe(to).Value);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));

        return builder;
    }
}