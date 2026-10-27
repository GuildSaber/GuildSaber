using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BeatLeaderHmdIdJsonConverter))]
public readonly record struct BeatLeaderHmdId
{
    private readonly int _value;

    private BeatLeaderHmdId(int value)
        => _value = value;

    public static Result<BeatLeaderHmdId> TryCreate(int? value) => value switch
    {
        null => Failure<BeatLeaderHmdId>("BeatLeaderHmdId must not be null"),
        < 0 => Failure<BeatLeaderHmdId>("BeatLeaderHmdId must not be negative"),
        _ => Success(new BeatLeaderHmdId(value.Value))
    };

    public static Result<BeatLeaderHmdId> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BeatLeaderHmdId>("BeatLeaderHmdId must be a number.");

    public static implicit operator int(BeatLeaderHmdId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderHmdId? CreateUnsafe(int? value)
        => value is null ? null : new BeatLeaderHmdId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class BeatLeaderHmdIdJsonConverter : JsonConverter<BeatLeaderHmdId>
{
    public override BeatLeaderHmdId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return BeatLeaderHmdId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return BeatLeaderHmdId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to BeatLeaderHmdId");
    }

    public override void Write(Utf8JsonWriter writer, BeatLeaderHmdId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}