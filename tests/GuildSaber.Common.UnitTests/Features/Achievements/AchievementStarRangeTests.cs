using AwesomeAssertions;
using GuildSaber.Api.Features.LegacyGS.Pipelines;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.RankedMaps;

namespace GuildSaber.Common.UnitTests.Features.Achievements;

public class AchievementStarRangeTests
{
    [Test]
    public void StarAchievements_ShouldBeUnboundedByDefault()
    {
        var diffAchievement = new DiffStarAchievement(
            default, default, default, null, default, default, 0, false, default,
            new RankedMapRating.DifficultyStar(5), 1);
        var accAchievement = new AccStarAchievement(
            default, default, default, null, default, default, 0, false, default,
            new RankedMapRating.AccuracyStar(5), 1);

        diffAchievement.MaxStar.Should().BeNull();
        accAchievement.MaxStar.Should().BeNull();
    }

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