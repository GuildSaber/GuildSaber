using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.Server.StrongTypes;

public readonly record struct BeatLeaderId
{
    private readonly ulong _value;

    private BeatLeaderId(ulong value)
        => _value = value;

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.xyz/player/{id}/exists";

    public static implicit operator ulong(BeatLeaderId id)
        => id._value;

    public static Task<Result<Maybe<BeatLeaderId>>> CreateAsync(ulong value, HttpClient httpClient)
        => ExistOnRemote(value.ToString(), httpClient)
            .Map(exists => exists ? From(new BeatLeaderId(value)) : None);

    private static Task<Result<bool>> ExistOnRemote(string id, HttpClient httpClient)
        => Try(() => httpClient.GetAsync(VerificationUrl(id)))
            .Map(response => response.IsSuccessStatusCode);


    [return: NotNullIfNotNull(nameof(value))]
    public static BeatLeaderId? CreateUnsafe(ulong? value)
        => value is null ? null : new BeatLeaderId(value.Value);
}