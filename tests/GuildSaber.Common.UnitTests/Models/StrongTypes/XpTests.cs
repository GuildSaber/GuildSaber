using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Common.StrongTypes;

namespace GuildSaber.Common.UnitTests.Models.StrongTypes;

public class XpTests
{
    [Test]
    public void TryCreate_ShouldAcceptBoundaryValues()
    {
        Xp.TryCreate(Xp.MinValue).IsSuccess.Should().BeTrue();
        Xp.TryCreate(Xp.MaxValue).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void TryCreate_ShouldRejectNegativeAndNonFiniteValues()
    {
        foreach (var value in new[]
                 {
                     Xp.MinValue - 1,
                     float.NaN,
                     float.NegativeInfinity,
                     float.PositiveInfinity
                 })
            Xp.TryCreate(value).IsFailure.Should().BeTrue();
    }

    [Test]
    public void JsonConverter_ShouldRoundTripAsNumber()
    {
        var value = Xp.TryCreate(42.5f).Value;

        var json = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<Xp>(json);

        json.Should().Be("42.5");
        ((float)deserialized).Should().Be(42.5f);
    }

    [Test]
    public void JsonConverter_ShouldRejectInvalidValuesAndTokenTypes()
    {
        Action deserializeNegative = () => JsonSerializer.Deserialize<Xp>("-1");
        Action deserializeString = () => JsonSerializer.Deserialize<Xp>("\"42\"");

        deserializeNegative.Should().Throw<JsonException>();
        deserializeString.Should().Throw<JsonException>();
    }
}