using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

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
    {
        if (reader.TokenType is not JsonTokenType.Number || !reader.TryGetInt32(out var rawValue))
            throw new JsonException("MaxScore must be a JSON integer within the Int32 range.");

        return MaxScore.TryCreate(rawValue).TryGetValue(out var value, out var error)
            ? value
            : throw new JsonException(error);
    }

    public override void Write(Utf8JsonWriter writer, MaxScore value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}