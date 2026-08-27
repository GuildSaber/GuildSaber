using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

[JsonConverter(typeof(NJSJsonConverter))]
public readonly record struct NJS
{
    private readonly float _value;

    private NJS(float value)
        => _value = value;

    public static Result<NJS> TryCreate(float? value)
        => value is null
            ? Failure<NJS>("NJS must not be null")
            : Success(new NJS(value.Value));

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
        => NJS.TryCreate(reader.GetSingle()).TryGetValue(out var value)
            ? value
            : throw new JsonException("Cannot convert to NJS.");

    public override void Write(Utf8JsonWriter writer, NJS value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}