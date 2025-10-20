using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

[JsonConverter(typeof(BeatSaverKeyJsonConverter))]
public readonly record struct BeatSaverKey
{
    public const int MaxLength = 6;

    private readonly string _value;

    private BeatSaverKey(string value)
        => _value = value;

    public static implicit operator string(BeatSaverKey id)
        => id._value;

    public static Result<BeatSaverKey> TryCreate(string? value) => value switch
    {
        null => Failure<BeatSaverKey>("BeatSaverKey must not be null."),
        { Length: > MaxLength } => Failure<BeatSaverKey>($"BeatSaverKey must be at most {MaxLength} characters long."),
        _ when !value.All(char.IsLetterOrDigit) => Failure<BeatSaverKey>("BeatSaverKey must be alphanumeric."),
        _ => Success(new BeatSaverKey(value))
    };

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatSaverKey? CreateUnsafe(string? value)
        => value is null ? null : new BeatSaverKey(value!);

    public override string ToString()
        => _value;
}

public class BeatSaverKeyJsonConverter : JsonConverter<BeatSaverKey>
{
    public override BeatSaverKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Cannot convert to BeatSaverKey");

        var result = BeatSaverKey.TryCreate(reader.GetString());
        return result.IsSuccess
            ? result.Value
            : throw new JsonException($"Cannot convert to BeatSaverKey: {result.Error}");
    }

    public override void Write(Utf8JsonWriter writer, BeatSaverKey value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}