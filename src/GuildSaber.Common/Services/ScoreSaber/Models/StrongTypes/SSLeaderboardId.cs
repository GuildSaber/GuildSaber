using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

[JsonConverter(typeof(SSLeaderboardIdJsonConverter))]
public readonly record struct SSLeaderboardId
{
    private readonly int _value;

    private SSLeaderboardId(int value)
        => _value = value;

    public static Result<SSLeaderboardId> TryCreate(int? value) => value switch
    {
        null => Failure<SSLeaderboardId>("SSLeaderboardId must not be null"),
        < 1 => Failure<SSLeaderboardId>("SSLeaderboardId must be greater than 0"),
        _ => Success(new SSLeaderboardId(value.Value))
    };

    public static Result<SSLeaderboardId> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<SSLeaderboardId>("SSLeaderboardId must be a number.");

    public static implicit operator int(SSLeaderboardId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static SSLeaderboardId? CreateUnsafe(int? value)
        => value is null ? null : new SSLeaderboardId(value.Value);

    public override string ToString()
        => _value.ToString();
}

public class SSLeaderboardIdJsonConverter : JsonConverter<SSLeaderboardId>
{
    public override SSLeaderboardId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return SSLeaderboardId.CreateUnsafe(value).Value;

        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            return SSLeaderboardId.CreateUnsafe(stringValue).Value;

        throw new JsonException("Cannot convert to SSLeaderboardId");
    }

    public override void Write(Utf8JsonWriter writer, SSLeaderboardId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}