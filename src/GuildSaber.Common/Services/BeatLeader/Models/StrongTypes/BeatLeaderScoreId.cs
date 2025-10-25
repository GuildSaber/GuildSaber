using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

public readonly record struct BeatLeaderScoreId
{
    private readonly int _value;

    private BeatLeaderScoreId(int value)
        => _value = value;

    public static Result<BeatLeaderScoreId> TryCreate(int? value) => value switch
    {
        null => Failure<BeatLeaderScoreId>("BeatLeaderScoreId must not be null"),
        < 1 => Failure<BeatLeaderScoreId>("BeatLeaderScoreId must be greater than 0"),
        _ => Success(new BeatLeaderScoreId(value.Value))
    };

    public static Result<BeatLeaderScoreId> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BeatLeaderScoreId>("BeatLeaderScoreId must be a number.");

    public static implicit operator int(BeatLeaderScoreId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderScoreId? CreateUnsafe(int? value)
        => value is null ? null : new BeatLeaderScoreId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class BeatLeaderScoreIdJsonConverter : JsonConverter<BeatLeaderScoreId>
{
    public override BeatLeaderScoreId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return BeatLeaderScoreId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return BeatLeaderScoreId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to BeatLeaderScoreId");
    }

    public override void Write(Utf8JsonWriter writer, BeatLeaderScoreId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}