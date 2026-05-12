using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(CategoryIdJsonConverter))]
public readonly record struct CategoryId(int Value)
{
    public static bool TryParse(string from, out CategoryId value)
    {
        if (int.TryParse(from, out var id))
        {
            value = new CategoryId(id);
            return true;
        }

        value = default;
        return false;
    }

    public static implicit operator int(CategoryId id)
        => id.Value;

    public override string ToString()
        => Value.ToString();
}

public class CategoryIdJsonConverter : JsonConverter<CategoryId>
{
    public override CategoryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new CategoryId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new CategoryId(value);

        throw new JsonException("Cannot convert to CategoryId");
    }

    public override void Write(Utf8JsonWriter writer, CategoryId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}