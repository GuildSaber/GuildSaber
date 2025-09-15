using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

public readonly record struct Accuracy
{
    private readonly float _value;

    private Accuracy(float value)
        => _value = value;

    public static Result<Accuracy> TryCreate(float? value)
        => value switch
        {
            null => Failure<Accuracy>("Accuracy must not be null"),
            < 0 or > 100 => Failure<Accuracy>("Accuracy must be between 0 and 100"),
            _ => Success(new Accuracy(value.Value))
        };

    public static Result<Accuracy> TryParse(string? value)
        => float.TryParse(value, out var parsed)
            ? TryCreate(parsed)
            : Failure<Accuracy>("Accuracy must be a number.");

    public static implicit operator float(Accuracy id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static Accuracy? CreateUnsafe(float? value)
        => value is null ? null : new Accuracy(value.Value);

    public static Accuracy From(BaseScore score, MaxScore maxScore)
        => new((float)score / maxScore * 100f);

    public static bool operator >=(Accuracy left, Accuracy right)
        => left._value >= right._value;

    public static bool operator <=(Accuracy left, Accuracy right)
        => left._value <= right._value;

    public static bool operator >(Accuracy left, Accuracy right)
        => left._value > right._value;

    public static bool operator <(Accuracy left, Accuracy right)
        => left._value < right._value;

    public override string ToString()
        => _value.ToString();
}