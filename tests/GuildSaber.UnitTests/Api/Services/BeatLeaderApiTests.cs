using System.Linq.Expressions;
using FluentAssertions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.UnitTests.Utils;

namespace GuildSaber.UnitTests.Api.Services;

public class BeatLeaderApiTests
{
    private readonly BeatLeaderApi _beatLeaderApi;
    private readonly BeatLeaderId _invalidBeatLeaderId = BeatLeaderId.CreateUnsafe(99999999999).Value;
    private readonly BeatLeaderId _validBeatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value;

    public BeatLeaderApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.beatleader.com/");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _beatLeaderApi = new BeatLeaderApi(httpClient);
    }

    [Fact]
    public async Task GetPlayerScoresCompact_ShouldReturnNullAndStopIteration_WhenInvalidPlayerId()
    {
        // Arrange
        var playerId = _invalidBeatLeaderId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<ScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = Order.Desc,
            SortBy = ScoresSortBy.Date
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompact(playerId, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            compactScores.Should().BeNull("because the player ID is invalid");
        }
    }

    [Theory]
    [InlineData(ScoresSortBy.Date, Order.Desc), InlineData(ScoresSortBy.Acc, Order.Asc)]
    [InlineData(ScoresSortBy.Acc, Order.Desc), InlineData(ScoresSortBy.Date, Order.Asc)]
    public async Task GetPlayerScoresCompact_ShouldReturnScoresInCorrectSortingOrder_WhenOrderByAndSortBySpecified(
        ScoresSortBy sortBy, Order orderBy)
    {
        // Arrange
        var playerId = _validBeatLeaderId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<ScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            SortBy = sortBy,
            Order = orderBy
        };

        var scores = new List<CompactScoreResponse>();

        // Act
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompact(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            scores.AddRange(compactScores);
        }

        // Arrange Assertion
        var compFunc = (Expression<Func<CompactScoreResponse, IComparable>>)(sortBy switch
        {
            ScoresSortBy.Date => x => x.Score.EpochTime,
            ScoresSortBy.Acc => x => x.Score.Accuracy,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });

        // Assert
        scores.Should()
            .BeInOrders(compFunc, orderBy)
            .And.HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Fact]
    public async Task GetPlayerScoresCompact_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        var playerId = _validBeatLeaderId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<ScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = Order.Desc,
            SortBy = ScoresSortBy.Date
        };

        // Act
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompact(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            compactScores.Should().NotBeNullOrEmpty("because we expect to receive scores");
        }
    }

    [Fact]
    public async Task GetPlayerProfile_ShouldReturnNull_WhenInvalidPlayerId()
    {
        // Arrange
        var playerId = _invalidBeatLeaderId;

        // Act
        var profile = await _beatLeaderApi.GetPlayerProfile(playerId);

        // Assert
        profile.Should().SucceedWith(null);
    }
}