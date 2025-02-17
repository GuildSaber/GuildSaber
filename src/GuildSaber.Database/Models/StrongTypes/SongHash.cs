using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct SongHash
{
    private readonly string _value;

    private SongHash(string value)
        => _value = value;

    public static implicit operator string(SongHash id)
        => id._value;

    public static Result<SongHash> TryCreate(string? value)
        => value switch
        {
            null => Failure<SongHash>("Song hash must not be null."),
           { Length: not 40 } => Failure<SongHash>("Song hash must be 40 characters long."),
            _ when !value.All(char.IsLetterOrDigit) => Failure<SongHash>("Song hash must be alphanumeric."),
            _ => Success(new SongHash(value))
        };

    public static SongHash? CreateUnsafe(string? value)
        => value is null ? null : new SongHash(value);
}