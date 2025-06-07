using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct Name_2_6
{
    public const int MaxLength = 6;
    public const int MinLength = 2;
    public const string Name = "Name";

    private readonly string _value;

    private Name_2_6(string value)
        => _value = value;

    public static implicit operator string(Name_2_6 id)
        => id._value;

    public static Result<Name_2_6> TryCreate(string? value, string name = Name)
        => value?.Trim() switch
        {
            null => Failure<Name_2_6>($"{name} must not be null."),
            { Length: > MaxLength } => Failure<Name_2_6>($"{name} must be at most {MaxLength} of length."),
            { Length: < MinLength } => Failure<Name_2_6>($"{name} must be at least {MinLength} of length."),
            var x => Success(new Name_2_6(x))
        };

    [return: NotNullIfNotNull(nameof(value))]
    public static Name_2_6? CreateUnsafe(string? value)
        => value is null ? null : new Name_2_6(value);

    public override string ToString()
        => _value;
}