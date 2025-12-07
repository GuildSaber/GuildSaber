using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(GuildIdJsonConverter))]
public readonly record struct GuildId(int Value)
{
    public static bool TryParse(string from, out GuildId value)
    {
        if (int.TryParse(from, out var id))
        {
            value = new GuildId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator int(GuildId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class GuildIdJsonConverter : JsonConverter<GuildId>
{
    public override GuildId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new GuildId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new GuildId(value);

        throw new JsonException("Cannot convert to GuildId");
    }

    public override void Write(Utf8JsonWriter writer, GuildId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}