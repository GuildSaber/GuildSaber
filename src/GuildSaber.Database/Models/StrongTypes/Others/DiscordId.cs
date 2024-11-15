namespace GuildSaber.Database.Models.StrongTypes.Implements;

public readonly record struct DiscordId
{
    private readonly ulong _value;
    
    private DiscordId(ulong value)
        => _value = value;
    
    public DiscordId? TryCreate(ulong value)
        => value == 0 ? null : new DiscordId(value);
    
    public DiscordId? TryParse(string value)
        => ulong.TryParse(value, out var parsed) ? TryCreate(parsed) : null;

    public static implicit operator ulong(DiscordId id)
        => id._value;
}