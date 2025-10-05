using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct RawPoints
{
    private readonly float _value;

    private RawPoints(float value)
        => _value = value;

    public static Result<RawPoints> TryCreate(float? value) => value switch
    {
        null => Failure<RawPoints>("RawPoints must not be null"),
        < 0 => Failure<RawPoints>("RawPoints must not be negative"),
        _ => Success(new RawPoints(value.Value))
    };

    public static Result<RawPoints> TryParse(string? value)
        => float.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<RawPoints>("RawPoints must be a number.");

    public static implicit operator float(RawPoints id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static RawPoints? CreateUnsafe(float? value)
        => value is null ? null : new RawPoints(value.Value);

    public override string ToString()
        => _value.ToString(CultureInfo.InvariantCulture);
}