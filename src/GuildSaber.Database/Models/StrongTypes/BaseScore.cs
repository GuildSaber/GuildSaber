using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct BaseScore
{
    private readonly int _value;

    private BaseScore(int value)
        => _value = value;

    public static Result<BaseScore> TryCreate(int? value) => value switch
    {
        null => Failure<BaseScore>("BaseScore must not be null"),
        < 0 => Failure<BaseScore>("BaseScore must be non-negative."),
        _ => Success(new BaseScore(value.Value))
    };

    public static Result<BaseScore> TryParse(string? value)
        => int.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<BaseScore>("BaseScore must be a number.");

    public static implicit operator int(BaseScore id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static BaseScore? CreateUnsafe(int? value)
        => value is null ? null : new BaseScore(value.Value);

    public override string ToString()
        => _value.ToString();
}