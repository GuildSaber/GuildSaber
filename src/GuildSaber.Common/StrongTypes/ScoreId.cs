using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(ScoreIdJsonConverter))]
public readonly record struct ScoreId(int Value)
{
    public static bool TryParse(string from, out ScoreId value)
    {
        if (int.TryParse(from, out var id))
        {
            value = new ScoreId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator int(ScoreId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class ScoreIdJsonConverter : JsonConverter<ScoreId>
{
    public override ScoreId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return int.TryParse(propertyName, out var value)
            ? new ScoreId(value)
            : throw new JsonException($"Cannot convert '{propertyName}' to ScoreId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, ScoreId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override ScoreId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new ScoreId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new ScoreId(value);

        throw new JsonException("Cannot convert to ScoreId");
    }

    public override void Write(Utf8JsonWriter writer, ScoreId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}