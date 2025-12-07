using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct DiscordGuildId
{
    private readonly ulong _value;

    private DiscordGuildId(ulong value)
        => _value = value;

    public static Result<DiscordGuildId> TryCreate(ulong value)
        => value == 0
            ? Failure<DiscordGuildId>("DiscordGuildId must not be 0.")
            : Success(new DiscordGuildId(value));

    public static Result<DiscordGuildId> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<DiscordGuildId>("DiscordGuildId must be a number.");

    public static implicit operator ulong(DiscordGuildId id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static DiscordGuildId? CreateUnsafe(ulong? value)
        => value is null ? null : new DiscordGuildId(value.Value);

    public override string ToString()
        => _value.ToString();
}