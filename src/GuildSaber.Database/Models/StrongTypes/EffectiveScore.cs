using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct EffectiveScore
{
    private readonly ulong _value;

    private EffectiveScore(ulong value)
        => _value = value;

    public static Result<EffectiveScore> TryCreate(ulong? value)
        => value is null
            ? Failure<EffectiveScore>("EffectiveScore must not be null")
            : Success(new EffectiveScore(value.Value));

    public static Result<EffectiveScore> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<EffectiveScore>("EffectiveScore must be a number.");

    public static implicit operator ulong(EffectiveScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static EffectiveScore? CreateUnsafe(ulong? value)
        => value is null ? null : new EffectiveScore(value.Value);

    public override string ToString()
        => _value.ToString();
}