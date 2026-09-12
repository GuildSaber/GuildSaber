using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(XpJsonConverter))]
public readonly record struct Xp
{
    public const float MinValue = 0;
    public const float MaxValue = float.MaxValue;

    private readonly float _value;

    private Xp(float value)
        => _value = value;

    public static Result<Xp> TryCreate(float? value) => value switch
    {
        null => Failure<Xp>("Xp must not be null"),
        { } actual when !float.IsFinite(actual) => Failure<Xp>("Xp must be finite"),
        < MinValue => Failure<Xp>("Xp must not be negative"),
        _ => Success(new Xp(value.GetValueOrDefault()))
    };

    public static Result<Xp> TryParse(string? value)
        => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? TryCreate(parsed)
            : Failure<Xp>("Xp must be a number.");

    public static implicit operator float(Xp id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static Xp? CreateUnsafe(float? value)
        => value is null ? null : new Xp(value.GetValueOrDefault());

    public static bool operator >=(Xp left, Xp right)
        => left._value >= right._value;

    public static bool operator <=(Xp left, Xp right)
        => left._value <= right._value;

    public static bool operator >(Xp left, Xp right)
        => left._value > right._value;

    public static bool operator <(Xp left, Xp right)
        => left._value < right._value;

    public override string ToString()
        => _value.ToString(CultureInfo.InvariantCulture);
}

public sealed class XpJsonConverter : JsonConverter<Xp>
{
    public override Xp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.Number || !reader.TryGetSingle(out var rawValue))
            throw new JsonException("Xp must be a JSON number.");

        return Xp.TryCreate(rawValue).Match(
            value => value,
            error => throw new JsonException(error));
    }

    public override void Write(Utf8JsonWriter writer, Xp value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}