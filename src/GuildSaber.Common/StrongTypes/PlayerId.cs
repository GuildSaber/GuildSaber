using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(PlayerIdJsonConverter))]
public readonly record struct PlayerId(int Value)
{
    public static bool TryParse(string from, out PlayerId value)
    {
        if (int.TryParse(from, out var id))
        {
            value = new PlayerId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator int(PlayerId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class PlayerIdJsonConverter : JsonConverter<PlayerId>
{
    public override PlayerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new PlayerId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new PlayerId(value);

        throw new JsonException("Cannot convert to PlayerId");
    }

    public override void Write(Utf8JsonWriter writer, PlayerId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}