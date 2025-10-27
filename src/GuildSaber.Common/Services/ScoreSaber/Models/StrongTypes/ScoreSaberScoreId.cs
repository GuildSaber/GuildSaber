using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

[JsonConverter(typeof(ScoreSaberScoreIdJsonConverter))]
public readonly record struct ScoreSaberScoreId
{
    private readonly int _value;

    private ScoreSaberScoreId(int value)
        => _value = value;

    public static Result<ScoreSaberScoreId> TryCreate(int? value) => value switch
    {
        null => Failure<ScoreSaberScoreId>("ScoreSaberScoreId must not be null"),
        < 1 => Failure<ScoreSaberScoreId>("ScoreSaberScoreId must be greater than 0"),
        _ => Success(new ScoreSaberScoreId(value.Value))
    };

    public static Result<ScoreSaberScoreId> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<ScoreSaberScoreId>("ScoreSaberScoreId must be a number.");

    public static implicit operator int(ScoreSaberScoreId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static ScoreSaberScoreId? CreateUnsafe(int? value)
        => value is null ? null : new ScoreSaberScoreId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class ScoreSaberScoreIdJsonConverter : JsonConverter<ScoreSaberScoreId>
{
    public override ScoreSaberScoreId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return ScoreSaberScoreId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return ScoreSaberScoreId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to ScoreSaberScoreId");
    }

    public override void Write(Utf8JsonWriter writer, ScoreSaberScoreId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}