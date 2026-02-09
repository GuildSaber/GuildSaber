using AwesomeAssertions;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.LegacyGuildSaber;
using GuildSaber.Common.Services.LegacyGuildSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;

namespace GuildSaber.Common.UnitTests.Services.OldGuildSaber;

public class LegacyGuildSaberApiTests
{
    private readonly GuildId _validGuildId = new(1);
    private readonly LegacyGuildSaberApi _legacyGuildSaberApi;

    public LegacyGuildSaberApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _legacyGuildSaberApi = new LegacyGuildSaberApi(httpClient);
    }

    [Test]
    public async Task GetGuildRankedDifficulties_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        var guildId = _validGuildId;
        var requestOptions = new LegacyGuildSaberApi.PaginatedRequestOptions<RankedMapsSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Reverse = false,
            SortBy = RankedMapsSortBy.EditedTime
        };

        // Act
        await foreach (var data in _legacyGuildSaberApi.GetGuildRankedMaps(guildId, requestOptions))
        {
            if (!data.TryGetValue(out var rankedMaps, out var error))
                Assert.Fail(error);

            rankedMaps!.Should().NotBeNullOrEmpty("because we expect to receive ranked maps");
        }
    }

    [Test]
    public async Task GetRankingLevels_ShouldReturnAtLeast10Levels_WhenValidGuildId()
    {
        // Arrange
        var guildId = _validGuildId;

        // Act
        var result = await _legacyGuildSaberApi.GetRankingLevelsAsync(guildId);

        // Assert
        if (!result.TryGetValue(out var rankingLevels, out var error))
            Assert.Fail(error);

        rankingLevels!.Should()
            .HaveCountGreaterThanOrEqualTo(10, "because guild 1 should have at least 10 ranking levels");
    }

    [Test]
    public async Task GetRankingCategories_ShouldReturnAtLeast10Levels_WhenValidGuildId()
    {
        // Arrange
        var guildId = _validGuildId;

        // Act
        var result = await _legacyGuildSaberApi.GetRankingCategoriesAsync(guildId);

        // Assert
        if (!result.TryGetValue(out var rankingLevels, out var error))
            Assert.Fail(error);

        rankingLevels!.Should()
            .HaveCountGreaterThanOrEqualTo(3, "because guild 1 should have at least 3 ranking categories");
    }

    [Test]
    public async Task GetRankedScoreStateAsync_ShouldReturnAllowedState_WhenScoreIsValid()
    {
        var guildId = _validGuildId;
        var beatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value;
        var scoreSaberId = ScoreSaberId.CreateUnsafe(76561198126131670).Value;
        var ssid = 287616;
        var blid = "d8d091";
        var unmodifiedScore = 1319764;

        // Act
        var result = await _legacyGuildSaberApi.GetRankedScoreStateAsync(
            guildId, beatLeaderId, scoreSaberId, blid, ssid, unmodifiedScore);

        // Assert
        if (!result.TryGetValue(out var state, out var error))
            Assert.Fail(error);

        state.Should().Be(EState.Allowed, "because the score should be valid and allowed");
    }

    [Test]
    public async Task GetRankedScoreStateAsync_ShouldReturnFailure_WhenScoreIsInvalid()
    {
        var guildId = _validGuildId;
        var beatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value;
        var scoreSaberId = ScoreSaberId.CreateUnsafe(76561198126131670).Value;
        var ssid = 287616;
        var blid = "d8d091";
        var unmodifiedScore = 1; // Invalid score

        // Act
        var result = await _legacyGuildSaberApi.GetRankedScoreStateAsync(
            guildId, beatLeaderId, scoreSaberId, blid, ssid, unmodifiedScore);

        // Assert
        result.Should().Fail("because the score is invalid and shouldn't be found on LegacyGuildSaber");
    }
}