using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

[JsonConverter(typeof(SSGameModeJsonConverter))]
public readonly record struct SSGameMode
{
    private readonly string _value;

    private SSGameMode(string value)
        => _value = value;

    public static implicit operator string(SSGameMode id)
        => id._value;

    public static Result<SSGameMode> TryCreate(string? value)
        => TransformValue(value) switch
        {
            null => Failure<SSGameMode>("ScoreSaberGameMode must not be null"),
            var transformed => Success(new SSGameMode(transformed))
        };

    private static string? TransformValue(string? value) => value switch
    {
        "Standard" => "SoloStandard",
        not null => value,
        _ => null
    };

    [return: NotNullIfNotNull(nameof(value))]
    public static SSGameMode? CreateUnsafe(string? value)
        => value is null ? null : new SSGameMode(TransformValue(value)!);

    public override string ToString()
        => _value;
}

public class SSGameModeJsonConverter : JsonConverter<SSGameMode>
{
    public override SSGameMode Read(
        ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.String
            ? SSGameMode.CreateUnsafe(reader.GetString()!).Value
            : throw new JsonException("Cannot convert to ScoreSaberGameMode");

    public override void Write(
        Utf8JsonWriter writer, SSGameMode value,
        JsonSerializerOptions options) => writer.WriteStringValue(value);
}