using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

[JsonConverter(typeof(MaxScoreJsonConverter))]
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

public sealed class MaxScoreJsonConverter : JsonConverter<MaxScore>
{
    public override MaxScore Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => MaxScore.TryCreate(reader.GetInt32()).TryGetValue(out var value)
            ? value
            : throw new JsonException("Cannot convert to MaxScore.");

    public override void Write(Utf8JsonWriter writer, MaxScore value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}