using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(RankedMapIdJsonConverter))]
public readonly record struct RankedMapId(long Value)
{
    public static bool TryParse(string from, out RankedMapId value)
    {
        if (long.TryParse(from, out var id))
        {
            value = new RankedMapId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator long(RankedMapId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class RankedMapIdJsonConverter : JsonConverter<RankedMapId>
{
    public override RankedMapId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
                                                   JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return long.TryParse(propertyName, out var value)
            ? new RankedMapId(value)
            : throw new JsonException($"Cannot convert '{propertyName}' to RankedMapId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, RankedMapId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override RankedMapId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var stringValue))
            return new RankedMapId(stringValue);

        throw new JsonException("Cannot convert to RankedMapId");
    }

    public override void Write(Utf8JsonWriter writer, RankedMapId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}