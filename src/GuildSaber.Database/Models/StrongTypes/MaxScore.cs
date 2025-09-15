using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct MaxScore
{
    private readonly ulong _value;

    private MaxScore(ulong value)
        => _value = value;

    public static Result<MaxScore> TryCreate(ulong? value)
        => value is null
            ? Failure<MaxScore>("MaxScore must not be null")
            : Success(new MaxScore(value.Value));

    public static Result<MaxScore> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<MaxScore>("MaxScore must be a number.");

    public static implicit operator ulong(MaxScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static MaxScore? CreateUnsafe(ulong? value)
        => value is null ? null : new MaxScore(value.Value);

    public override string ToString()
        => _value.ToString();
}