using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct DiscordId
{
    private readonly ulong _value;

    private DiscordId(ulong value)
        => _value = value;

    public static Result<DiscordId> TryCreate(ulong value)
        => value == 0
            ? Failure<DiscordId>("Discord ID must not be 0.")
            : Success(new DiscordId(value));

    public static Result<DiscordId> TryParse(string value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordId>("Discord ID must be a number.");

    public static implicit operator ulong(DiscordId id)
        => id._value;

    public static DiscordId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordId(value.Value);
}