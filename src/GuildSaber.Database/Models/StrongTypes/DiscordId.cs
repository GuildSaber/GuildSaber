using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct DiscordId
{
    private readonly ulong _value;

    private DiscordId(ulong value)
        => _value = value;

    public static Result<DiscordId> TryCreate(ulong value)
        => value == 0
            ? Failure<DiscordId>("Discord ID must not be 0.")
            : Success(new DiscordId(value));

    public static Result<DiscordId> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordId>("Discord ID must be a number.");

    public static implicit operator ulong(DiscordId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static DiscordId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class DiscordIdJsonConverter : JsonConverter<DiscordId>
{
    public override DiscordId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return ulong.TryParse(propertyName, out var value)
            ? DiscordId.CreateUnsafe(value).Value
            : throw new JsonException($"Cannot convert '{propertyName}' to DiscordId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DiscordId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override DiscordId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            ulong.TryParse(reader.GetString(), out var stringValue))
            return DiscordId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return DiscordId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to DiscordId");
    }

    public override void Write(Utf8JsonWriter writer, DiscordId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}