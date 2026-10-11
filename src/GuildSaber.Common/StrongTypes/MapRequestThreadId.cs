using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(MapRequestThreadIdJsonConverter))]
public readonly record struct MapRequestThreadId(long Value)
{
    public static bool TryParse(string from, out MapRequestThreadId value)
    {
        if (long.TryParse(from, out var id))
        {
            value = new MapRequestThreadId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator long(MapRequestThreadId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class MapRequestThreadIdJsonConverter : JsonConverter<MapRequestThreadId>
{
    public override MapRequestThreadId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
                                                          JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return long.TryParse(propertyName, out var value)
            ? new MapRequestThreadId(value)
            : throw new JsonException($"Cannot convert '{propertyName}' to MapRequestThreadId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, MapRequestThreadId value,
                                             JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override MapRequestThreadId Read(ref Utf8JsonReader reader, Type typeToConvert,
                                            JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var stringValue))
            return new MapRequestThreadId(stringValue);

        throw new JsonException("Cannot convert to MapRequestThreadId");
    }

    public override void Write(Utf8JsonWriter writer, MapRequestThreadId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}