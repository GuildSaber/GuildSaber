using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct BaseScore
{
    private readonly ulong _value;

    private BaseScore(ulong value)
        => _value = value;

    public static Result<BaseScore> TryCreate(ulong? value)
        => value is null
            ? Failure<BaseScore>("BaseScore must not be null")
            : Success(new BaseScore(value.Value));

    public static Result<BaseScore> TryParse(string? value)
        => ulong.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BaseScore>("BaseScore must be a number.");

    public static implicit operator ulong(BaseScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BaseScore? CreateUnsafe(ulong? value)
        => value is null ? null : new BaseScore(value.Value);

    public override string ToString()
        => _value.ToString();
}