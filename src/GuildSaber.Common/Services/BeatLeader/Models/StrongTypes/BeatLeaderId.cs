using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using GuildSaber.Common.StrongTypes;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

[JsonConverter(typeof(BeatLeaderIdJsonConverter))]
[StructLayout(LayoutKind.Explicit)]
public readonly struct BeatLeaderId
{
    [FieldOffset(0)]
    public readonly Platform Kind;

    [FieldOffset(sizeof(Platform))]
    private readonly SteamId _steamId;

    [FieldOffset(sizeof(Platform))]
    private readonly MetaPCId _metaPCId;

    [FieldOffset(sizeof(Platform))]
    private readonly BLNativeId _blNativeId;

    public BeatLeaderId() => throw new InvalidOperationException(
        "BeatLeaderId cannot be default. Use the constructor with a valid ID.");

    private BeatLeaderId(SteamId steamId) => (_steamId, Kind) = (steamId, Platform.Steam);
    private BeatLeaderId(MetaPCId metaPCId) => (_metaPCId, Kind) = (metaPCId, Platform.MetaPC);
    private BeatLeaderId(BLNativeId blNativeId) => (_blNativeId, Kind) = (blNativeId, Platform.MetaNative);

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.com/player/{id}/exists";

    public static Result<BeatLeaderId> TryParse(string? from)
        => !ulong.TryParse(from, out var parsed)
            ? Failure<BeatLeaderId>($"Invalid BeatLeaderId: {from}. It must be a valid unsigned long integer.")
            : TryCreate(parsed);

    public static Result<BeatLeaderId> TryCreate(ulong value)
        => SteamId.TryCreate(value).Map(id => (BeatLeaderId)id)
            .Compensate(_ => MetaPCId.TryCreate(value).Map(id => (BeatLeaderId)id))
            .Compensate(_ => BLNativeId.TryCreate(value).Map(id => (BeatLeaderId)id));

    public static Task<Result<Maybe<BeatLeaderId>>> CreateAsync(ulong value, HttpClient httpClient)
        => ExistOnRemote(value.ToString(), httpClient)
            .Map(static (exists, id) => exists ? TryCreate(id).AsMaybe() : None, context: value);

    private static Task<Result<bool>> ExistOnRemote(string id, HttpClient httpClient)
        => Try(() => httpClient.GetAsync(VerificationUrl(id)))
            .Map(response => response.IsSuccessStatusCode);

    /// <remarks>Used for minimal API Deserialization</remarks>
    public static bool TryParse(string? from, out BeatLeaderId id)
    {
        var result = TryParse(from);
        if (result.IsSuccess)
        {
            id = result.Value;
            return true;
        }

        id = default;
        return false;
    }

    /// <remarks>
    /// I know I could just use whatever field because they all overlap, but in case the type size changes, better be explicit.
    /// </remarks>
    public static implicit operator ulong(BeatLeaderId id) => id.Kind switch
    {
        Platform.Steam => id._steamId,
        Platform.MetaPC => id._metaPCId,
        Platform.MetaNative => id._blNativeId,
        _ => throw new InvalidOperationException("Invalid BeatLeaderId kind.")
    };

    public static implicit operator BeatLeaderId(SteamId steamId) => new(steamId);
    public static implicit operator BeatLeaderId(MetaPCId metaPCId) => new(metaPCId);
    public static implicit operator BeatLeaderId(BLNativeId blNativeId) => new(blNativeId);

    public override string ToString() => ((ulong)this).ToString();

    public enum Platform : byte
    {
        Steam,
        MetaPC,
        MetaNative
    }
}

public class BeatLeaderIdJsonConverter : JsonConverter<BeatLeaderId>
{
    public override BeatLeaderId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => !BeatLeaderId.TryParse(reader.GetString())
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to BeatLeaderId: {error}")
                : id,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => !BeatLeaderId.TryCreate(value)
                .TryGetValue(out var id, out var error)
                ? throw new JsonException($"Cannot convert to BeatLeaderId: {error}")
                : id,
            _ => throw new JsonException("Cannot convert to BeatLeaderId")
        };

    public override void Write(Utf8JsonWriter writer, BeatLeaderId value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}