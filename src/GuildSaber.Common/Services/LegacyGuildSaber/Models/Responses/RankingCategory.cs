using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses;

public class RankingCategory
{
    public required LegacyCategoryId Id { get; init; }
    public required int GuildId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Logo { get; init; }

    [JsonConverter(typeof(LegacyCategoryJsonConverter))]
    public readonly record struct LegacyCategoryId(int Value)
    {
        public static bool TryParse(string from, out LegacyCategoryId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new LegacyCategoryId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(LegacyCategoryId id)
            => id.Value;

        public override string ToString()
            => Value.ToString();
    }
}

public class LegacyCategoryJsonConverter : JsonConverter<RankingCategory.LegacyCategoryId>
{
    public override RankingCategory.LegacyCategoryId Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new RankingCategory.LegacyCategoryId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new RankingCategory.LegacyCategoryId(value);

        throw new JsonException("Cannot convert to LegacyCategoryId");
    }

    public override void Write(
        Utf8JsonWriter writer, RankingCategory.LegacyCategoryId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}