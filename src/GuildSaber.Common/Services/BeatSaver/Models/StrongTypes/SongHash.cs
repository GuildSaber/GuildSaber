using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

[JsonConverter(typeof(SongHashJsonConverter))]
public readonly record struct SongHash
{
    public const int ExactLength = 40;
    private readonly string _value;

    private SongHash(string value)
        => _value = value;

    public static implicit operator string(SongHash id)
        => id._value;

    public static Result<SongHash> TryCreate(string? value) => value switch
    {
        null => Failure<SongHash>("Song hash must not be null."),
        { Length: not ExactLength } => Failure<SongHash>($"Song hash must be {ExactLength} characters long."),
        _ when !value.All(char.IsLetterOrDigit) => Failure<SongHash>("Song hash must be alphanumeric."),
        _ => Success(new SongHash(value))
    };

    [return: NotNullIfNotNull(nameof(value))]
    public static SongHash? CreateUnsafe(string? value)
        => value is null ? null : new SongHash(value);

    public override string ToString()
        => _value;
}

public class SongHashJsonConverter : JsonConverter<SongHash>
{
    public override SongHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Cannot convert to SongHash");

        var result = SongHash.TryCreate(reader.GetString());
        return result.IsSuccess ? result.Value : throw new JsonException($"Cannot convert to SongHash: {result.Error}");
    }

    public override void Write(Utf8JsonWriter writer, SongHash value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}