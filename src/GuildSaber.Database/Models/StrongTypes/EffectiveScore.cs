using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct EffectiveScore : IComparable<EffectiveScore>
{
    private readonly int _value;

    private EffectiveScore(int value)
        => _value = value;

    public int CompareTo(EffectiveScore other) => _value.CompareTo(other._value);

    public static Result<EffectiveScore> TryCreate(int? value) => value switch
    {
        null => Failure<EffectiveScore>("EffectiveScore must not be null"),
        < 0 => Failure<EffectiveScore>("EffectiveScore must be greater than or equal to 0"),
        _ => Success(new EffectiveScore(value.Value))
    };

    public static Result<EffectiveScore> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<EffectiveScore>("EffectiveScore must be a number.");

    public static implicit operator int(EffectiveScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static EffectiveScore? CreateUnsafe(int? value)
        => value is null ? null : new EffectiveScore(value.Value);

    public override string ToString()
        => _value.ToString();
}