using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes.Others;

public readonly record struct BLLeaderboardId
{
    private readonly string _value;

    private BLLeaderboardId(string value)
        => _value = value;
    
    public static implicit operator string(BLLeaderboardId id)
        => id._value;

    public static Result<BLLeaderboardId> TryCreate(string value) 
        => new BLLeaderboardId(value);
    
    public static BLLeaderboardId? CreateUnsafe(string? value)
        => value is null ? null : new BLLeaderboardId(value);
}