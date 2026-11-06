using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace GuildSaber.Database.Models.StrongTypes;

[JsonConverter(typeof(AccuracyJsonConverter))]
public readonly record struct Accuracy
{
    private readonly double _value;

    private Accuracy(double value)
        => _value = value;

    public static Result<Accuracy> TryCreate(double? value) => value switch
    {
        null => Failure<Accuracy>("Accuracy must not be null"),
        { } actual when !double.IsFinite(actual) => Failure<Accuracy>("Accuracy must be finite"),
        < 0 or > 100 => Failure<Accuracy>("Accuracy must be between 0 and 100"),
        _ => Success(new Accuracy(value.Value))
    };

    public static Result<Accuracy> TryParse(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? TryCreate(parsed)
            : Failure<Accuracy>("Accuracy must be a number.");

    public static implicit operator double(Accuracy id)
        => id._value;

    [return: NotNullIfNotNull(nameof(value))]
    public static Accuracy? CreateUnsafe(double? value)
        => value is null ? null : new Accuracy(value.Value);

    public static Result<Accuracy> From(BaseScore score, MaxScore maxScore)
        => TryCreate((double)score / maxScore * 100d);

    public static bool operator >=(Accuracy left, Accuracy right)
        => left._value >= right._value;

    public static bool operator <=(Accuracy left, Accuracy right)
        => left._value <= right._value;

    public static bool operator >(Accuracy left, Accuracy right)
        => left._value > right._value;

    public static bool operator <(Accuracy left, Accuracy right)
        => left._value < right._value;

    public override string ToString()
        => _value.ToString(CultureInfo.InvariantCulture);
}

public sealed class AccuracyJsonConverter : JsonConverter<Accuracy>
{
    public override Accuracy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => (reader.TokenType switch
        {
            JsonTokenType.String => Accuracy.TryParse(reader.GetString()),
            JsonTokenType.Number when reader.TryGetDouble(out var rawValue) => Accuracy.TryCreate(rawValue),
            _ => Failure<Accuracy>("Accuracy must be a JSON number or numeric string.")
        }).Match(value => value, error => throw new JsonException(error));

    public override void Write(Utf8JsonWriter writer, Accuracy value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
