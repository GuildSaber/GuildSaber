using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(MapProposalIdJsonConverter))]
public readonly record struct MapProposalId(long Value)
{
    public static bool TryParse(string from, out MapProposalId value)
    {
        if (long.TryParse(from, out var id))
        {
            value = new MapProposalId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator long(MapProposalId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class MapProposalIdJsonConverter : JsonConverter<MapProposalId>
{
    public override MapProposalId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
                                                     JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return long.TryParse(propertyName, out var value)
            ? new MapProposalId(value)
            : throw new JsonException($"Cannot convert '{propertyName}' to MapProposalId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, MapProposalId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override MapProposalId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && long.TryParse(reader.GetString(), out var stringValue))
            return new MapProposalId(stringValue);

        throw new JsonException("Cannot convert to MapProposalId");
    }

    public override void Write(Utf8JsonWriter writer, MapProposalId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}