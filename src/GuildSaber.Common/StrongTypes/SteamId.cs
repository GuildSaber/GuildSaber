using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(SteamIdJsonConverter))]
public readonly record struct SteamId
{
    /// <summary>
    /// A steamId is a 17-digit number, so the minimum value is 10^16.
    /// </summary>
    public const ulong MinValue = 10_000_000_000_000_000;

    /// <summary>
    /// A steamId is a 17-digit number, so the maximum value is 10^17 - 1.
    /// </summary>
    public const ulong MaxValue = 99_999_999_999_999_999;

    private readonly ulong _value;
    private SteamId(ulong value) => _value = value;

    public static Result<SteamId> TryParse(string? value) => value switch
    {
        null => Failure<SteamId>("SteamId must not be null."),
        _ => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<SteamId>("SteamId must be a number.")
    };

    public static Result<SteamId> TryCreate(ulong value)
        => value is < MinValue or > MaxValue
            ? Failure<SteamId>($"SteamId must be between {MinValue} and {MaxValue}.")
            : Success(new SteamId(value));

    [return: NotNullIfNotNull(nameof(value))]
    public static SteamId? CreateUnsafe(ulong? value)
        => value is null ? null : new SteamId(value.Value);

    public static implicit operator ulong(SteamId id) => id._value;
    public override string ToString() => _value.ToString();
}

public class SteamIdJsonConverter : JsonConverter<SteamId>
{
    public override SteamId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => !SteamId.TryParse(reader.GetString())
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to SteamId: {error}")
                : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !SteamId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to SteamId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to SteamId")
        };

    public override void Write(Utf8JsonWriter writer, SteamId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public class NullableSteamIdJsonConverter : JsonConverter<SteamId?>
{
    public override SteamId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => string.IsNullOrWhiteSpace(reader.GetString())
                ? null
                : !SteamId.TryParse(reader.GetString())
                    .TryGetValue(out var id, out var error)
                    ? throw new JsonException($"Cannot convert to SteamId: {error}")
                    : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !SteamId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to SteamId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to SteamId")
        };

    public override void Write(Utf8JsonWriter writer, SteamId? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}