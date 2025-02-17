using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct BeatSaverKey
{
    private readonly string _value;

    private BeatSaverKey(string value)
        => _value = value;
    
    public static implicit operator string(BeatSaverKey id)
        => id._value;

    public static Result<BeatSaverKey> TryCreate(string value) 
        => new BeatSaverKey(value);
    
    public static BeatSaverKey? CreateUnsafe(string? value)
        => value is null ? null : new BeatSaverKey(value!);
}