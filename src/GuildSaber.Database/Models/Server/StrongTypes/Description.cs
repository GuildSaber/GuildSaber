using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.Server.StrongTypes;

public readonly record struct Description
{
    public const int MaxLength = 256;
    private readonly string _value;

    private Description(string value)
        => _value = value;

    public static implicit operator string(Description id)
        => id._value;

    public static Result<Description> TryCreate(string? value)
        => value?.Trim() switch
        {
            null => Failure<Description>("Description must not be null."),
            { Length: > MaxLength } => Failure<Description>($"Description must be at most {MaxLength} of length."),
            { Length: 0 } => Failure<Description>("Description must not be empty or whitespace."),
            var x => Success(new Description(x))
        };

    [return: NotNullIfNotNull(nameof(value))]
    public static Description? CreateUnsafe(string? value)
        => value is null ? null : new Description(value);
}