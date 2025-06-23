using System.Drawing;
using CSharpFunctionalExtensions;
using GuildSaber.Database.Extensions;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuildSaber.Database.Models.Server.Guilds;

public readonly record struct GuildInfo
{
    public required Name_5_50 Name { get; init; }
    public required Name_2_6 SmallName { get; init; }
    public required Description Description { get; init; }
    public required Color Color { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static Result<GuildInfo> TryCreate
        (string name, string smallName, string description, Color color, DateTimeOffset createdAt)
        => (name: Name_5_50.TryCreate(name), smallName: Name_2_6.TryCreate(smallName),
                description: Description.TryCreate(description), color, createdAt) switch
            {
                { name: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { smallName: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { description: { IsFailure: true, Error: var error } } => Failure<GuildInfo>(error),
                { color.IsEmpty: true } => Failure<GuildInfo>("GuildInfo.Color must not be empty"),
                { createdAt.UtcTicks: 0 } => Failure<GuildInfo>("GuildInfo.CreatedAt must not be 0"),
                var x => Success(new GuildInfo
                {
                    Name = x.name.Value,
                    SmallName = x.smallName.Value,
                    Description = x.description.Value,
                    Color = x.color,
                    CreatedAt = x.createdAt
                })
            };
}

public class GuildInfoConfiguration : IComplexPropertyConfiguration<GuildInfo>
{
    public ComplexPropertyBuilder<GuildInfo> Configure(ComplexPropertyBuilder<GuildInfo> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Name_5_50.MaxLength)
            .HasConversion<string>(from => from, to => Name_5_50.CreateUnsafe(to).Value);
        builder.Property(x => x.SmallName).HasMaxLength(Name_2_6.MaxLength)
            .HasConversion<string>(from => from, to => Name_2_6.CreateUnsafe(to).Value);
        builder.Property(x => x.Description).HasMaxLength(Description.MaxLength)
            .HasConversion<string>(from => from, to => Description.CreateUnsafe(to).Value);
        builder.Property(x => x.Color).HasConversion(from => from.ToArgb(), to => Color.FromArgb(to));

        return builder;
    }
}