using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.Server.StrongTypes;

public readonly record struct SmallName
{
    public const int MaxLength = 6;
    public const int MinLength = 2;
    private readonly string _value;

    private SmallName(string value)
        => _value = value;

    public static implicit operator string(SmallName id)
        => id._value;

    public static Result<SmallName> TryCreate(string? value)
        => value?.Trim() switch
        {
            null => Failure<SmallName>("SmallName must not be null."),
            { Length: > MaxLength } => Failure<SmallName>($"SmallName must be at most {MaxLength} of length."),
            { Length: < MinLength } => Failure<SmallName>($"SmallName must be at least {MinLength} of length."),
            var x => Success(new SmallName(x))
        };

    [return: NotNullIfNotNull(nameof(value))]
    public static SmallName? CreateUnsafe(string? value)
        => value is null ? null : new SmallName(value);
}