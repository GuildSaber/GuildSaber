using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(DiscordChannelIdJsonConverter))]
public readonly record struct DiscordChannelId
{
    private readonly ulong _value;

    private DiscordChannelId(ulong value)
        => _value = value;

    public static Result<DiscordChannelId> TryCreate(ulong value)
        => value == 0
            ? Failure<DiscordChannelId>("DiscordChannelId must not be 0.")
            : Success(new DiscordChannelId(value));

    public static Result<DiscordChannelId> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordChannelId>("DiscordChannelId must be a number.");

    public static implicit operator ulong(DiscordChannelId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static DiscordChannelId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordChannelId(value.Value);

    public static bool TryParse(string? from, out DiscordChannelId value)
    {
        if (ulong.TryParse(from, out var id))
        {
            value = new DiscordChannelId(id);
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString()
        => _value.ToString();
}

public class DiscordChannelIdJsonConverter : JsonConverter<DiscordChannelId>
{
    public override DiscordChannelId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return ulong.TryParse(propertyName, out var value)
            ? DiscordChannelId.CreateUnsafe(value).Value
            : throw new JsonException($"Cannot convert '{propertyName}' to DiscordChannelId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DiscordChannelId value,
                                             JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override DiscordChannelId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), out var stringValue))
            return DiscordChannelId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return DiscordChannelId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to DiscordChannelId");
    }

    public override void Write(Utf8JsonWriter writer, DiscordChannelId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}