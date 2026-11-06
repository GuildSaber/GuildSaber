using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.StrongTypes;

namespace GuildSaber.Common.UnitTests.Models.StrongTypes;

public class AccuracyTests
{
    [Test]
    public void TryCreate_ShouldPreserveDoublePrecisionWithinPercentageBounds()
    {
        foreach (var percentage in new[] { 0d, 95.123456789123, 100d })
            ((double)Accuracy.TryCreate(percentage).Value).Should().Be(percentage);

        Accuracy.TryParse("95.123456789123").Value.Should().Be(Accuracy.TryCreate(95.123456789123).Value);
    }

    [Test]
    public void TryCreate_ShouldRejectNullOutOfRangeAndNonFiniteValues()
    {
        Accuracy.TryCreate(null).IsFailure.Should().BeTrue();

        foreach (var percentage in new[] { -1d, 101d, double.NaN, double.NegativeInfinity, double.PositiveInfinity })
            Accuracy.TryCreate(percentage).IsFailure.Should().BeTrue();
    }

    [Test]
    public void From_ShouldValidateScoreAccuracyAndPreservePercentageUnits()
    {
        var maximum = MaxScore.TryCreate(1000).Value;

        ((double)Accuracy.From(BaseScore.TryCreate(950).Value, maximum).Value).Should().Be(95);
        ((double)Accuracy.From(default, maximum).Value).Should().Be(0);
        Accuracy.From(BaseScore.TryCreate(1001).Value, maximum).IsFailure.Should().BeTrue();
        Accuracy.From(default, default).IsFailure.Should().BeTrue();
    }

    [Test]
    [Arguments("en-US")]
    [Arguments("fr-FR")]
    public void JsonConverter_ShouldReadNumbersAndNumericStringsRegardlessOfCulture(string culture)
    {
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

            foreach (var (json, expected) in new[]
                     {
                         ("0", 0d), ("95.5", 95.5d), ("95.123456789123", 95.123456789123d),
                         ("100", 100d), ("9.55e1", 95.5d)
                     })
            {
                var fromNumber = JsonSerializer.Deserialize<Accuracy>(json);
                var fromString = JsonSerializer.Deserialize<Accuracy>(JsonSerializer.Serialize(json));

                ((double)fromNumber).Should().Be(expected);
                fromString.Should().Be(fromNumber);

                var serialized = JsonSerializer.Serialize(fromString);

                using var document = JsonDocument.Parse(serialized);

                document.RootElement.ValueKind.Should().Be(JsonValueKind.String);
                JsonSerializer.Deserialize<Accuracy>(serialized).Should().Be(fromString);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void JsonConverter_ShouldRejectInvalidValuesAndTokenTypes()
    {
        foreach (var json in new[]
                 {
                     "null", "-1", "101", "1e400", "true", "[]", "{}",
                     "\"-1\"", "\"101\"", "\"1e400\"", "\"NaN\"", "\"Infinity\"", "\"-Infinity\"",
                     "\"\"", "\"abc\"", "\"95,5\"", "\"0,095\""
                 })
        {
            Action deserialize = () => JsonSerializer.Deserialize<Accuracy>(json);

            deserialize.Should().Throw<JsonException>();
        }
    }
}
