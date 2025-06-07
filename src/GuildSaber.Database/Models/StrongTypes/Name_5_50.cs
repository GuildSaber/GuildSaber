using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct Name_5_50
{
    public const int MaxLength = 50;
    public const int MinLength = 5;
    public const string Name = "Name";

    private readonly string _value;

    private Name_5_50(string value)
        => _value = value;

    public static implicit operator string(Name_5_50 id)
        => id._value;

    public static Result<Name_5_50> TryCreate(string? value, string name = Name)
        => value?.Trim() switch
        {
            null => Failure<Name_5_50>($"{name} must not be null."),
            { Length: > MaxLength } => Failure<Name_5_50>($"{name} must be at most {MaxLength} of length."),
            { Length: < MinLength } => Failure<Name_5_50>($"{name} must be at least {MinLength} of length."),
            var x => Success(new Name_5_50(x))
        };

    [return: NotNullIfNotNull(nameof(value))]
    public static Name_5_50? CreateUnsafe(string? value)
        => value is null ? null : new Name_5_50(value);

    public override string ToString()
        => _value;
}