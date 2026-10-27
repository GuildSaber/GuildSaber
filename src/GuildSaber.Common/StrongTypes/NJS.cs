using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(NJSJsonConverter))]
public readonly record struct NJS
{
    private readonly float _value;

    private NJS(float value)
        => _value = value;

    public static Result<NJS> TryCreate(float? value) => value switch
    {
        null => Failure<NJS>("NJS must not be null"),
        { } actual when !float.IsFinite(actual) => Failure<NJS>("NJS must be finite"),
        _ => Success(new NJS(value.Value))
    };

    public static Result<NJS> TryParse(string? value)
        => float.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<NJS>("NJS must be a number.");

    public static implicit operator float(NJS id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static NJS? CreateUnsafe(float? value)
        => value is null ? null : new NJS(value.Value);

    public override string ToString()
        => _value.ToString();
}

public sealed class NJSJsonConverter : JsonConverter<NJS>
{
    public override NJS Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.Number || !reader.TryGetSingle(out var rawValue))
            throw new JsonException("NJS must be a JSON number.");

        return NJS.TryCreate(rawValue).TryGetValue(out var value, out var error)
            ? value
            : throw new JsonException(error);
    }

    public override void Write(Utf8JsonWriter writer, NJS value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}