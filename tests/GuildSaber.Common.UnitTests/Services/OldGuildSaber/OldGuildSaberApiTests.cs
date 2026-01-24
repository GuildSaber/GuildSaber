using AwesomeAssertions;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.OldGuildSaber;
using GuildSaber.Common.Services.OldGuildSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Common.UnitTests.Services.OldGuildSaber;

public class OldGuildSaberApiTests
{
    private const int ValidGuildId = 1;
    private readonly OldGuildSaberApi _oldGuildSaberApi;

    public OldGuildSaberApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _oldGuildSaberApi = new OldGuildSaberApi(httpClient);
    }

    [Test]
    public async Task GetGuildRankedDifficulties_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        var guildId = ValidGuildId;
        var requestOptions = new OldGuildSaberApi.PaginatedRequestOptions<RankedMapsSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Reverse = false,
            SortBy = RankedMapsSortBy.EditedTime
        };

        // Act
        await foreach (var data in _oldGuildSaberApi.GetGuildRankedMaps(guildId, requestOptions))
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
        var guildId = ValidGuildId;

        // Act
        var result = await _oldGuildSaberApi.GetRankingLevelsAsync(guildId);

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
        var guildId = ValidGuildId;

        // Act
        var result = await _oldGuildSaberApi.GetRankingCategoriesAsync(guildId);

        // Assert
        if (!result.TryGetValue(out var rankingLevels, out var error))
            Assert.Fail(error);

        rankingLevels!.Should()
            .HaveCountGreaterThanOrEqualTo(3, "because guild 1 should have at least 3 ranking categories");
    }

    [Test]
    public async Task GetRankedScoreStateAsync_ShouldReturnAllowedState_WhenScoreIsValid()
    {
        var beatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value;
        var scoreSaberId = ScoreSaberId.CreateUnsafe(76561198126131670).Value;
        var ssid = 287616;
        var blid = "d8d091";
        var unmodifiedScore = 1319764;

        // Act
        var result = await _oldGuildSaberApi.GetRankedScoreStateAsync(
            beatLeaderId, scoreSaberId, blid, ssid, unmodifiedScore);

        // Assert
        if (!result.TryGetValue(out var state, out var error))
            Assert.Fail(error);

        state.Should().Be(EState.Allowed, "because the score should be valid and allowed");
    }
}