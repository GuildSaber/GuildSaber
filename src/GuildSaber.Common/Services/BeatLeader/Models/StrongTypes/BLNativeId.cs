using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BLNativeIdJsonConverter))]
public readonly record struct BLNativeId
{
    /// <summary>
    /// A BL NativeId is an auto-incremented int starting at 1, so the theoritical MaxValue would never exceed 100M.
    /// </summary>
    public const ulong MaxValue = 100_000_000;

    private readonly ulong _value;
    private BLNativeId(ulong value) => _value = value;

    public static Result<BLNativeId> TryParse(string? value) => value switch
    {
        null => Failure<BLNativeId>("BLNativeId must not be null."),
        _ => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BLNativeId>("BLNativeId must be a number.")
    };

    public static Result<BLNativeId> TryCreate(ulong value)
        => value >= MaxValue
            ? Failure<BLNativeId>($"BLNativeId must be less than {MaxValue}.")
            : Success(new BLNativeId(value));

    [return: NotNullIfNotNull(nameof(value))]
    public static BLNativeId? CreateUnsafe(ulong? value)
        => value is null ? null : new BLNativeId(value.Value);

    public static implicit operator ulong(BLNativeId id) => id._value;
    public override string ToString() => _value.ToString();
}

public class BLNativeIdJsonConverter : JsonConverter<BLNativeId>
{
    public override BLNativeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => !BLNativeId.TryParse(reader.GetString())
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to BLNativeId: {error}")
                : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !BLNativeId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to BLNativeId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to BLNativeId")
        };

    public override void Write(Utf8JsonWriter writer, BLNativeId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

public class NullableBLNativeIdJsonConverter : JsonConverter<BLNativeId?>
{
    public override BLNativeId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => string.IsNullOrWhiteSpace(reader.GetString())
                ? null
                : !BLNativeId.TryParse(reader.GetString())
                    .TryGetValue(out var id, out var error)
                    ? throw new JsonException($"Cannot convert to BLNativeId: {error}")
                    : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !BLNativeId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to BLNativeId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to BLNativeId")
        };

    public override void Write(Utf8JsonWriter writer, BLNativeId? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue(value.Value);
    }
}