using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(DiscordRoleIdJsonConverter))]
public readonly record struct DiscordRoleId
{
    private readonly ulong _value;

    private DiscordRoleId(ulong value)
        => _value = value;

    public static Result<DiscordRoleId> TryCreate(ulong? value) => value switch
    {
        null => Failure<DiscordRoleId>("DiscordRoleId must not be null."),
        0 => Failure<DiscordRoleId>("DiscordRoleId must not be 0."),
        _ => Success(new DiscordRoleId(value.Value))
    };

    public static Result<DiscordRoleId> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordRoleId>("DiscordRoleId must be a number.");

    public static implicit operator ulong(DiscordRoleId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static DiscordRoleId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordRoleId(value.Value);

    public static bool TryParse(string? from, out DiscordRoleId value)
    {
        if (ulong.TryParse(from, out var id))
        {
            value = new DiscordRoleId(id);
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString()
        => _value.ToString();
}

public class DiscordRoleIdJsonConverter : JsonConverter<DiscordRoleId>
{
    public override DiscordRoleId ReadAsPropertyName(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var propertyName = reader.GetString();
        return ulong.TryParse(propertyName, out var value)
            ? DiscordRoleId.CreateUnsafe(value).Value
            : throw new JsonException($"Cannot convert '{propertyName}' to DiscordRoleId");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DiscordRoleId value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());

    public override DiscordRoleId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), out var stringValue))
            return DiscordRoleId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return DiscordRoleId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to DiscordRoleId");
    }

    public override void Write(Utf8JsonWriter writer, DiscordRoleId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}