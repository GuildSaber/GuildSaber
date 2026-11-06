using AwesomeAssertions;
using GuildSaber.Api.Features.LegacyGS.Pipelines;

namespace GuildSaber.Common.UnitTests.Features.Achievements;

public class AchievementStarRangeTests
{
    [Test]
    public void LegacyExclusiveMaxStar_ShouldBeTheNextInteger()
    {
        LegacyGuildSaberMapImportPipeline.GetLegacyExclusiveMaxStar(5).Value.Should().Be(6);
        LegacyGuildSaberMapImportPipeline.GetLegacyExclusiveMaxStar(5.75f).Value.Should().Be(6);
    }

    [Test]
    public void LegacyProgressionOrder_ShouldExcludeLevelOneHundred()
    {
        LegacyGuildSaberMapImportPipeline.GetLegacyProgressionOrder(5).Should().Be(5);
        LegacyGuildSaberMapImportPipeline.GetLegacyProgressionOrder(100).Should().BeNull();
    }
}