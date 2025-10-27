using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BeatLeaderIdJsonConverter))]
public readonly record struct BeatLeaderId
{
    private readonly ulong _value;

    private BeatLeaderId(ulong value)
        => _value = value;

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.xyz/player/{id}/exists";

    public static implicit operator ulong(BeatLeaderId id)
        => id._value;

    public override string ToString()
        => _value.ToString();

    public static Task<Result<Maybe<BeatLeaderId>>> CreateAsync(ulong value, HttpClient httpClient)
        => ExistOnRemote(value.ToString(), httpClient)
            .Map(static (exists, id) => exists ? From(new BeatLeaderId(id)) : None, context: value);

    private static Task<Result<bool>> ExistOnRemote(string id, HttpClient httpClient)
        => Try(() => httpClient.GetAsync(VerificationUrl(id)))
            .Map(response => response.IsSuccessStatusCode);

    public static Result<BeatLeaderId> TryParseUnsafe(string? value)
        => ulong.TryParse(value, out var parsed)
            ? Success(new BeatLeaderId(parsed))
            : Failure<BeatLeaderId>($"Invalid BeatLeaderId: {value}. It must be a valid unsigned long integer.");

    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderId? CreateUnsafe(ulong? value)
        => value is null ? null : new BeatLeaderId(value.Value);
}

public class BeatLeaderIdJsonConverter : JsonConverter<BeatLeaderId>
{
    public override BeatLeaderId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), out var stringValue))
            return BeatLeaderId.CreateUnsafe(stringValue).Value;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var value))
            return BeatLeaderId.CreateUnsafe(value).Value;

        throw new JsonException("Cannot convert to BeatLeaderId");
    }

    public override void Write(Utf8JsonWriter writer, BeatLeaderId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}