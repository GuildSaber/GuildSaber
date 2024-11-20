using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes.Others;

public readonly record struct BLId
{
    private readonly ulong _value;

    private BLId(ulong value)
        => _value = value;
    
    public static implicit operator ulong(BLId id)
        => id._value;

    public static Task<Result<Maybe<BLId>, ValidationError>> CreateAsync(ulong value, HttpClient httpClient)
        => ExistOnRemote(value.ToString(), httpClient)
            .Map(exists => exists ? From(new BLId(value)) : None);

    private static Task<Result<bool, ValidationError>> ExistOnRemote(string id, HttpClient httpClient)
        => Try(() => httpClient.GetAsync(VerificationUrl(id)),
            exception => new ValidationError(
                nameof(BLId),
                "Failed to verify existence of BeatLeaderId",
                exception)
        ).Map(response => response.IsSuccessStatusCode);

    private static Func<string, string> VerificationUrl =>
        id => $"https://api.beatleader.xyz/player/{id}/existss";
    
    public static BLId? CreateUnsafe(ulong? value)
        => value is null ? null : new BLId(value.Value);
}