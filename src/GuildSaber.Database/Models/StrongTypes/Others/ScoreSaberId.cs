namespace GuildSaber.Database.Models.StrongTypes.Others;

public readonly record struct ScoreSaberId
{
    private readonly ulong _value;

    private ScoreSaberId(ulong value)
        => _value = value;

    public ScoreSaberId? TryCreate(ulong value)
        => value == 0 ? null : new ScoreSaberId(value);

    public ScoreSaberId? TryParse(string value)
        => ulong.TryParse(value, out var parsed) ? TryCreate(parsed) : null;

    public static implicit operator ulong(ScoreSaberId id)
        => id._value;
    
    public static ScoreSaberId? CreateUnsafe(ulong? value)
        => value is null ? null : new ScoreSaberId(value.Value);
}