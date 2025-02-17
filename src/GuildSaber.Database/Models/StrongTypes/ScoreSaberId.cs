using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct ScoreSaberId
{
    private readonly ulong _value;

    private ScoreSaberId(ulong value)
        => _value = value;

    public Result<ScoreSaberId> TryCreate(ulong? value)
        => value switch
        {
            null => Failure<ScoreSaberId>("ScoreSaber ID must not be null."),
            0 => Failure<ScoreSaberId>("ScoreSaber ID must not be 0."),
            _ => Success(new ScoreSaberId(value.Value))
        };

    public Result<ScoreSaberId> TryParse(string value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<ScoreSaberId>("ScoreSaber ID must be a number.");

    public static implicit operator ulong(ScoreSaberId id)
        => id._value;
    
    [return: NotNullIfNotNull(nameof(value))]
    public static ScoreSaberId? CreateUnsafe(ulong? value)
        => value is null ? null : new ScoreSaberId(value.Value);
}