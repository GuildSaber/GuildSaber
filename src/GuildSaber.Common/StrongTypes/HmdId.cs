using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(HmdIdJsonConverter))]
public readonly record struct HmdId(short Value)
{
    public static bool TryParse(string from, out HmdId value)
    {
        if (short.TryParse(from, out var id))
        {
            value = new HmdId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator short(HmdId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class HmdIdJsonConverter : JsonConverter<HmdId>
{
    public override HmdId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && short.TryParse(reader.GetString(), out var stringValue))
            return new HmdId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt16(out var value))
            return new HmdId(value);

        throw new JsonException("Cannot convert to HmdId");
    }

    public override void Write(Utf8JsonWriter writer, HmdId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}