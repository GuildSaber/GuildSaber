using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

[JsonConverter(typeof(ScoreSaberIdJsonConverter))]
public readonly record struct ScoreSaberId
{
    private readonly ulong _value;

    private ScoreSaberId(ulong value)
        => _value = value;

    public static Result<ScoreSaberId> TryCreate(ulong? value) => value switch
    {
        null => Failure<ScoreSaberId>("ScoreSaber ID must not be null."),
        0 => Failure<ScoreSaberId>("ScoreSaber ID must not be 0."),
        _ => Success(new ScoreSaberId(value.Value))
    };

    public static Result<ScoreSaberId> TryParse(string value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<ScoreSaberId>("ScoreSaber ID must be a number.");

    public static implicit operator ulong(ScoreSaberId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static ScoreSaberId? CreateUnsafe(ulong? value)
        => value is null ? null : new ScoreSaberId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class ScoreSaberIdJsonConverter : JsonConverter<ScoreSaberId>
{
    public override ScoreSaberId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), out var stringValue))
            return ScoreSaberId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return ScoreSaberId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to ScoreSaberId");
    }

    public override void Write(Utf8JsonWriter writer, ScoreSaberId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}