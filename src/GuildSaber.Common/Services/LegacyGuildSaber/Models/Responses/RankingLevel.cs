using System.Text.Json;
using System.Text.Json.Serialization;

namespace GuildSaber.Common.Services.LegacyGuildSaber.Models.Responses;

public class RankingLevel
{
    public required bool IsObtainable;
    public required LegacyLevelId Id { get; init; }
    public required int GuildId { get; init; }
    public required float LevelNumber { get; init; }
    public required bool UseName { get; init; }
    public required string Name { get; init; }
    public required float DefaultWeight { get; init; }
    public required ulong? DiscordRoleId { get; init; }
    public required string? Description { get; init; }
    public required int Color { get; init; }

    [JsonConverter(typeof(LegacyLevelJsonConverter))]
    public readonly record struct LegacyLevelId(int Id)
    {
        public static bool TryParse(string from, out LegacyLevelId value)
        {
            if (int.TryParse(from, out var id))
            {
                value = new LegacyLevelId(id);
                return true;
            }

            value = default;
            return false;
        }

        public static implicit operator int(LegacyLevelId id)
            => id.Id;

        public override string ToString()
            => Id.ToString();
    }
}

public class LegacyLevelJsonConverter : JsonConverter<RankingLevel.LegacyLevelId>
{
    public override RankingLevel.LegacyLevelId Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return new RankingLevel.LegacyLevelId(stringValue);

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return new RankingLevel.LegacyLevelId(value);

        throw new JsonException("Cannot convert to LegacyLevelId");
    }

    public override void Write(Utf8JsonWriter writer, RankingLevel.LegacyLevelId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}