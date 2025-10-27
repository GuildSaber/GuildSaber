using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BLLeaderboardIdJsonConverter))]
public readonly record struct BLLeaderboardId
{
    private readonly string _value;

    private BLLeaderboardId(string value)
        => _value = value;

    public static implicit operator string(BLLeaderboardId id)
        => id._value;

    public static Result<BLLeaderboardId> TryCreate(string value)
        => new BLLeaderboardId(value);

    [return: NotNullIfNotNull(nameof(value))]
    public static BLLeaderboardId? CreateUnsafe(string? value)
        => value is null ? null : new BLLeaderboardId(value);

    public override string ToString()
        => _value;
}

public class BLLeaderboardIdJsonConverter : JsonConverter<BLLeaderboardId>
{
    public override BLLeaderboardId Read(
        ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? BLLeaderboardId.CreateUnsafe(reader.GetString()!).Value
            : throw new JsonException("Cannot convert to BLLeaderboardId");

    public override void Write(
        Utf8JsonWriter writer, BLLeaderboardId value,
        JsonSerializerOptions options) => writer.WriteStringValue(value);
}