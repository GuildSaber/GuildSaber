using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;

public readonly record struct BeatLeaderId : IComparable<BeatLeaderId>
{
    private readonly ulong _value;

    private BeatLeaderId(ulong value)
        => _value = value;

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.xyz/player/{id}/exists";

    public int CompareTo(BeatLeaderId other) => _value.CompareTo(other._value);

    public static implicit operator ulong(BeatLeaderId id)
        => id._value;

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