using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct MaxScore
{
    private readonly int _value;

    private MaxScore(int value)
        => _value = value;

    public static Result<MaxScore> TryCreate(int? value) => value switch
    {
        null => Failure<MaxScore>("MaxScore must not be null"),
        < 1 => Failure<MaxScore>("MaxScore must be greater than 0"),
        _ => Success(new MaxScore(value.Value))
    };

    public static Result<MaxScore> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<MaxScore>("MaxScore must be a number.");

    public static implicit operator int(MaxScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static MaxScore? CreateUnsafe(int? value)
        => value is null ? null : new MaxScore(value.Value);

    public override string ToString()
        => _value.ToString();
}