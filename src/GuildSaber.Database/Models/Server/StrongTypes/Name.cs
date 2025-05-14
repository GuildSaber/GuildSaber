using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.Server.StrongTypes;

public readonly record struct Name
{
    public const int MaxLength = 50;
    public const int MinLength = 5;
    private readonly string _value;

    private Name(string value)
        => _value = value;

    public static implicit operator string(Name id)
        => id._value;

    public static Result<Name> TryCreate(string? value)
        => value?.Trim() switch
        {
            null => Failure<Name>("Name must not be null."),
            { Length: > MaxLength } => Failure<Name>($"Name must be at most {MaxLength} of length."),
            { Length: < MinLength } => Failure<Name>($"Name must be at least {MinLength} of length."),
            var x => Success(new Name(x))
        };

    [return: NotNullIfNotNull(nameof(value))]
    public static Name? CreateUnsafe(string? value)
        => value is null ? null : new Name(value);
}