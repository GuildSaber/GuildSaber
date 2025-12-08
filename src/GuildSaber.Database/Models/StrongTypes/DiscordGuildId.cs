using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

[JsonConverter(typeof(DiscordGuildIdJsonConverter))]
public readonly record struct DiscordGuildId
{
    private readonly ulong _value;

    private DiscordGuildId(ulong value)
        => _value = value;

    public static Result<DiscordGuildId> TryCreate(ulong value)
        => value == 0
            ? Failure<DiscordGuildId>("DiscordGuildId must not be 0.")
            : Success(new DiscordGuildId(value));

    public static Result<DiscordGuildId> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordGuildId>("DiscordGuildId must be a number.");

    public static implicit operator ulong(DiscordGuildId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static DiscordGuildId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordGuildId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class DiscordGuildIdJsonConverter : JsonConverter<DiscordGuildId>
{
    public override DiscordGuildId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return ulong.TryParse(propertyName, out var value)
            ? DiscordGuildId.CreateUnsafe(value).Value
            : throw new JsonException($"Cannot convert '{propertyName}' to DiscordGuildId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DiscordGuildId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override DiscordGuildId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), out var stringValue))
            return DiscordGuildId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return DiscordGuildId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to DiscordGuildId");
    }

    public override void Write(Utf8JsonWriter writer, DiscordGuildId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}