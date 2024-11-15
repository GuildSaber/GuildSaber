using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes.Others;

public readonly record struct BeatLeaderId
{
    private readonly ulong _value;

    private BeatLeaderId(ulong value)
        => _value = value;
    
    public static implicit operator ulong(BeatLeaderId id)
        => id._value;

    public static Task<Result<Maybe<BeatLeaderId>, ValidationError>> CreateAsync(ulong value, HttpClient httpClient)
        => ExistOnRemote(value.ToString(), httpClient)
            .Map(exists => exists ? From(new BeatLeaderId(value)) : None);

    private static Task<Result<bool, ValidationError>> ExistOnRemote(string id, HttpClient httpClient)
        => Try(() => httpClient.GetAsync(VerificationUrl(id)),
            exception => new ValidationError(
                nameof(BeatLeaderId),
                "Failed to verify existence of BeatLeaderId",
                exception)
        ).Map(response => response.IsSuccessStatusCode);

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.xyz/player/{id}/existss";
}