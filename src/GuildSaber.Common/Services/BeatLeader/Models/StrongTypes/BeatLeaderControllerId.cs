using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BeatLeaderControllerIdJsonConverter))]
public readonly record struct BeatLeaderControllerId
{
    private readonly int _value;

    private BeatLeaderControllerId(int value)
        => _value = value;

    public static Result<BeatLeaderControllerId> TryCreate(int? value) => value switch
    {
        null => Failure<BeatLeaderControllerId>("BeatLeaderControllerId must not be null"),
        < 0 => Failure<BeatLeaderControllerId>("BeatLeaderControllerId must not be negative"),
        _ => Success(new BeatLeaderControllerId(value.Value))
    };

    public static Result<BeatLeaderControllerId> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BeatLeaderControllerId>("BeatLeaderControllerId must be a number.");

    public static implicit operator int(BeatLeaderControllerId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderControllerId? CreateUnsafe(int? value)
        => value is null ? null : new BeatLeaderControllerId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class BeatLeaderControllerIdJsonConverter : JsonConverter<BeatLeaderControllerId>
{
    public override BeatLeaderControllerId Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return BeatLeaderControllerId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return BeatLeaderControllerId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to BeatLeaderControllerId");
    }

    public override void Write(Utf8JsonWriter writer, BeatLeaderControllerId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}