using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes.Others;

public readonly record struct SongHash
{
    private readonly string _value;

    private SongHash(string value)
        => _value = value;

    public static implicit operator string(SongHash id)
        => id._value;

    public static Result<SongHash, ValidationError> TryCreate(string value)
        => value.Length != 40
            ? new ValidationError(nameof(SongHash), "Song hash must be 40 characters long.", null)
            : new SongHash(value);

    public static SongHash? CreateUnsafe(string? value)
        => value is null ? null : new SongHash(value!);
}