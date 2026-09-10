using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.StrongTypes;

[JsonConverter(typeof(MetaPCIdJsonConverter))]
public readonly record struct MetaPCId
{
    /// <summary>
    /// A metaPCId is at least a 15-digit number, so the minimum value is 10^14.
    /// </summary>
    public const ulong MinValue = 100_000_000_000_000;

    /// <summary>
    /// A metaPCId is at most a 17-digit number under 7xx.
    /// </summary>
    public const ulong MaxValue = 69_999_999_999_999_999;

    private readonly ulong _value;
    private MetaPCId(ulong value) => _value = value;

    public static Result<MetaPCId> TryParse(string? value) => value switch
    {
        null => Failure<MetaPCId>("MetaPCId must not be null."),
        _ => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<MetaPCId>("MetaPCId must be a number.")
    };

    public static Result<MetaPCId> TryCreate(ulong value)
        => value is < MinValue or > MaxValue
            ? Failure<MetaPCId>($"MetaPCId must be between {MinValue} and {MaxValue}.")
            : Success(new MetaPCId(value));

    [return: NotNullIfNotNull(nameof(value))]
    public static MetaPCId? CreateUnsafe(ulong? value)
        => value is null ? null : new MetaPCId(value.Value);

    public static implicit operator ulong(MetaPCId id) => id._value;
    public override string ToString() => _value.ToString();
}

public class MetaPCIdJsonConverter : JsonConverter<MetaPCId>
{
    public override MetaPCId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => !MetaPCId.TryParse(reader.GetString())
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to MetaPCId: {error}")
                : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !MetaPCId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to MetaPCId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to MetaPCId")
        };

    public override void Write(Utf8JsonWriter writer, MetaPCId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public class NullableMetaPCIdJsonConverter : JsonConverter<MetaPCId?>
{
    public override MetaPCId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => string.IsNullOrWhiteSpace(reader.GetString())
                ? null
                : !MetaPCId.TryParse(reader.GetString())
                    .TryGetValue(out var id, out var error)
                    ? throw new JsonException($"Cannot convert to MetaPCId: {error}")
                    : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !MetaPCId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to MetaPCId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to MetaPCId")
        };

    public override void Write(Utf8JsonWriter writer, MetaPCId? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
