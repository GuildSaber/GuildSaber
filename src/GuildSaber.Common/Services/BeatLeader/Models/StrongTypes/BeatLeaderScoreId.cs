using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

public readonly record struct BeatLeaderScoreId : IComparable<BeatLeaderScoreId>
{
    private readonly uint _value;

    private BeatLeaderScoreId(uint value)
        => _value = value;

    public int CompareTo(BeatLeaderScoreId other) => _value.CompareTo(other._value);

    public static Result<BeatLeaderScoreId> TryCreate(uint? value)
        => value is null
            ? Failure<BeatLeaderScoreId>("BeatLeaderScoreId must not be null")
            : Success(new BeatLeaderScoreId(value.Value));

    public static Result<BeatLeaderScoreId> TryParse(string? value)
        => uint.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BeatLeaderScoreId>("BeatLeaderScoreId must be a number.");

    public static implicit operator uint(BeatLeaderScoreId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderScoreId? CreateUnsafe(uint? value)
        => value is null ? null : new BeatLeaderScoreId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class BeatLeaderScoreIdJsonConverter : JsonConverter<BeatLeaderScoreId>
{
    public override BeatLeaderScoreId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt32(out var value))
            return BeatLeaderScoreId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && uint.TryParse(reader.GetString(), out var stringValue))
            return BeatLeaderScoreId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to BeatLeaderScoreId");
    }

    public override void Write(Utf8JsonWriter writer, BeatLeaderScoreId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}