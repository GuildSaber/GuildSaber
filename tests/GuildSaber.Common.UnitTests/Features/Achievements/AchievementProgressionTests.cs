using System.Text.Json;
using AwesomeAssertions;
using static GuildSaber.Api.Features.Guilds.Achievements.Http.AchievementResponses;

namespace GuildSaber.Common.UnitTests.Features.Achievements;

public class AchievementProgressionTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    [Test]
    public void OrderedProgression_ShouldRoundTripThroughJson()
    {
        AchievementProgression progression = new AchievementProgression.Ordered(12, true);

        var json = JsonSerializer.Serialize(progression, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AchievementProgression>(json, _jsonOptions);

        json.Should().Contain("\"type\":\"Ordered\"");
        deserialized.Should().Be(progression);
    }

    [Test]
    public void UnorderedProgression_ShouldRoundTripThroughJson()
    {
        AchievementProgression progression = new AchievementProgression.Unordered();

        var json = JsonSerializer.Serialize(progression, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AchievementProgression>(json, _jsonOptions);

        json.Should().Contain("\"type\":\"Unordered\"");
        deserialized.Should().Be(progression);
    }
}