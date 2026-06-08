using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(RankedScoreIdJsonConverter))]
public readonly record struct RankedScoreId(long Value)
{
    public static bool TryParse(string from, out RankedScoreId value)
    {
        if (long.TryParse(from, out var id))
        {
            value = new RankedScoreId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator long(RankedScoreId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class RankedScoreIdJsonConverter : JsonConverter<RankedScoreId>
{
    public override RankedScoreId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return long.TryParse(propertyName, out var value)
            ? new RankedScoreId(value)
            : throw new JsonException($"Cannot convert '{propertyName}' to RankedScoreId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, RankedScoreId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override RankedScoreId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var stringValue))
            return new RankedScoreId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value))
            return new RankedScoreId(value);

        throw new JsonException("Cannot convert to RankedScoreId");
    }

    public override void Write(Utf8JsonWriter writer, RankedScoreId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}