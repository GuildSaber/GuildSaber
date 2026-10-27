using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(ControllerIdJsonConverter))]
public readonly record struct ControllerId(short Value)
{
    public static bool TryParse(string from, out ControllerId value)
    {
        if (short.TryParse(from, out var id))
        {
            value = new ControllerId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator short(ControllerId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class ControllerIdJsonConverter : JsonConverter<ControllerId>
{
    public override ControllerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && short.TryParse(reader.GetString(), out var stringValue))
            return new ControllerId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt16(out var value))
            return new ControllerId(value);

        throw new JsonException("Cannot convert to ControllerId");
    }

    public override void Write(Utf8JsonWriter writer, ControllerId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}