using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(ContextIdJsonConverter))]
public readonly record struct ContextId(int Value)
{
    public static bool TryParse(string? from, out ContextId value)
    {
        if (int.TryParse(from, out var id))
        {
            value = new ContextId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator int(ContextId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class ContextIdJsonConverter : JsonConverter<ContextId>
{
    public override ContextId Read(
        ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Number
            ? new ContextId(reader.GetInt32())
            : throw new JsonException("Cannot convert to ContextId");

    public override void Write(
        Utf8JsonWriter writer, ContextId value,
        JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}