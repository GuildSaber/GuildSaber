using System.Linq.Expressions;
using AwesomeAssertions;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models;
using GuildSaber.Common.Services.BeatLeader.Models.Responses;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Common.UnitTests.Utils;

namespace GuildSaber.Common.UnitTests.Services.BeatLeader;

public class BeatLeaderApiTests
{
    private readonly BeatLeaderApi _beatLeaderApi;
    private readonly BeatLeaderId _invalidBeatLeaderId = SteamId.CreateUnsafe(99999999999).Value;
    private readonly BeatLeaderScoreId _invalidBeatLeaderScoreId = BeatLeaderScoreId.CreateUnsafe(999999999).Value;
    private readonly BLLeaderboardId _invalidLeaderboardId = BLLeaderboardId.CreateUnsafe("0").Value;

    private readonly string _invalidSongCharacteristic = "InvalidMode";
    private readonly EDifficulty _invalidSongDifficulty = (EDifficulty)(-1);
    private readonly SongHash _invalidSongHash = SongHash.TryCreate("abcdef1234567890abcdef1234567890abcdef12").Value;

    private readonly BeatLeaderId _validBeatLeaderId = SteamId.CreateUnsafe(76561198126131670).Value;
    private readonly BeatLeaderScoreId _validBeatLeaderScoreId = BeatLeaderScoreId.CreateUnsafe(9655850).Value;
    private readonly BLLeaderboardId _validLeaderboardId = BLLeaderboardId.CreateUnsafe("a3c391").Value;

    private readonly string _validSongCharacteristic = "Standard";
    private readonly EDifficulty _validSongDifficulty = EDifficulty.ExpertPlus;
    private readonly SongHash _validSongHash = SongHash.TryCreate("c4ccc41a43bb15f252b025f03bce6f9c1dbbdbeb").Value;

    public BeatLeaderApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.beatleader.com/");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _beatLeaderApi = new BeatLeaderApi(httpClient);
    }

    [Test]
    public async Task GetPlayerScoresCompactAsyncEnumerable_ShouldReturnNullAndStopIteration_WhenInvalidPlayerId()
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
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompactAsyncEnumerable(playerId, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            compactScores.Should().BeNull("because the player ID is invalid");
        }
    }

    [Test]
    [Arguments(ScoresSortBy.Date, Order.Desc), Arguments(ScoresSortBy.Acc, Order.Asc)]
    [Arguments(ScoresSortBy.Acc, Order.Desc), Arguments(ScoresSortBy.Date, Order.Asc)]
    public async Task
        GetPlayerScoresCompactAsyncEnumerable_ShouldReturnScoresInCorrectSortingOrder_WhenOrderByAndSortBySpecified(
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
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompactAsyncEnumerable(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            scores.AddRange(compactScores!);
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
            .BeInOrders(compFunc, orderBy, "because the scores should be sorted according to the specified sortBy")
            .And.HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Test]
    public async Task GetPlayerScoresCompactAsyncEnumerable_ShouldReturnEmptyArray_WhenNoMoreData()
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
        await foreach (var data in _beatLeaderApi.GetPlayerScoresCompactAsyncEnumerable(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var compactScores))
                Assert.Fail(data.Error);

            compactScores.Should().NotBeNullOrEmpty("because we expect to receive scores");
        }
    }

    [Test]
    public async Task GetPlayerScoresAsyncEnumerable_ShouldReturnNullAndStopIteration_WhenInvalidPlayerId()
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
        await foreach (var data in _beatLeaderApi.GetPlayerScoresAsyncEnumerable(playerId, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().BeNull("because the player ID is invalid");
        }
    }

    [Test]
    [Arguments(ScoresSortBy.Date, Order.Desc), Arguments(ScoresSortBy.Acc, Order.Asc)]
    [Arguments(ScoresSortBy.Acc, Order.Desc), Arguments(ScoresSortBy.Date, Order.Asc)]
    public async Task
        GetPlayerScoresAsyncEnumerable_ShouldReturnScoresInCorrectSortingOrder_WhenOrderByAndSortBySpecified(
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

        var scores = new List<ScoreResponse>();

        // Act
        await foreach (var data in _beatLeaderApi.GetPlayerScoresAsyncEnumerable(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var scoreResponses))
                Assert.Fail(data.Error);

            scores.AddRange(scoreResponses!);
        }

        // Arrange Assertion
        var compFunc = (Expression<Func<ScoreResponse, IComparable>>)(sortBy switch
        {
            ScoresSortBy.Date => x => x.TimePost,
            ScoresSortBy.Acc => x => x.Accuracy,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });

        // Assert
        scores.Should()
            .BeInOrders(compFunc, orderBy, "because the scores should be sorted according to the specified sortBy")
            .And.HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Test]
    public async Task GetPlayerScoresAsyncEnumerable_ShouldReturnEmptyArray_WhenNoMoreData()
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
        await foreach (var data in _beatLeaderApi.GetPlayerScoresAsyncEnumerable(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().NotBeNullOrEmpty("because we expect to receive scores");
        }
    }

    [Test]
    public async Task GetPlayerProfile_ShouldReturnNull_WhenInvalidPlayerId()
    {
        // Arrange
        var playerId = _invalidBeatLeaderId;

        // Act
        var profile = await _beatLeaderApi.GetPlayerProfileAsync(playerId);

        // Assert
        profile.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetPlayerProfile_ShouldReturnValidProfile_WhenValidPlayerId()
    {
        // Arrange
        var playerId = _validBeatLeaderId;

        // Act
        var profile = await _beatLeaderApi.GetPlayerProfileAsync(playerId);

        // Assert
        profile.SuccessShould().NotBeNull("A valid player ID should return a non-null profile");
    }

    [Test]
    public async Task GetPlayerProfileWithStats_ShouldReturnNull_WhenInvalidPlayerId()
    {
        // Arrange
        var playerId = _invalidBeatLeaderId;

        // Act
        var profileWithStats = await _beatLeaderApi.GetPlayerProfileWithStatsAsync(playerId);

        // Assert
        profileWithStats.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetPlayerProfileWithStats_ShouldReturnValidProfile_WhenValidPlayerId()
    {
        // Arrange
        var playerId = _validBeatLeaderId;

        // Act
        var profileWithStats = await _beatLeaderApi.GetPlayerProfileWithStatsAsync(playerId);

        // Assert
        profileWithStats.SuccessShould().NotBeNull("A valid player ID should return a non-null profile with stats");
    }

    [Test]
    public async Task GetScoreStatistics_ShouldReturnNull_WhenInvalidScoreId()
    {
        // Arrange
        var scoreId = _invalidBeatLeaderScoreId;

        // Act
        var scoreStats = await _beatLeaderApi.GetScoreStatisticsAsync(scoreId);

        // Assert
        scoreStats.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetScoreStatistics_ShouldReturnValidStatistics_WhenValidScoreId()
    {
        // Arrange
        var scoreId = _validBeatLeaderScoreId;

        // Act
        var scoreStats = await _beatLeaderApi.GetScoreStatisticsAsync(scoreId);

        // Assert
        scoreStats.SuccessShould().NotBeNull("A valid score ID should return non-null score statistics");
    }

    [Test]
    public async Task GetExMachinaStars_ShouldReturnNull_WhenInvalidSongData()
    {
        // Act
        var exMachinaStars = await _beatLeaderApi.GetExMachinaStarRatingAsync(
            _invalidSongHash,
            _invalidSongDifficulty,
            _invalidSongCharacteristic
        );

        // Assert
        exMachinaStars.SuccessShould().BeNull("An invalid hash should return a null success response");
    }

    [Test]
    public async Task GetExMachinaStars_ShouldReturnValidStars_WhenValidSongData()
    {
        // Act
        var exMachinaStars = await _beatLeaderApi.GetExMachinaStarRatingAsync(
            _validSongHash,
            _validSongDifficulty,
            _validSongCharacteristic
        );

        // Assert
        exMachinaStars.SuccessShould().NotBeNull("A valid song data should return non-null ExMachina star ratings");
    }

    [Test]
    public async Task GetLeaderboards_ShouldReturnNull_WhenInvalidSongHash()
    {
        // Arrange
        var songHash = _invalidSongHash;

        // Act
        var leaderboards = await _beatLeaderApi.GetLeaderboardsAsync(songHash);

        // Assert
        leaderboards.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetLeaderboards_ShouldReturnValidLeaderboards_WhenValidSongHash()
    {
        // Arrange
        var songHash = _validSongHash;
        const EDifficulty expectedDifficulty = EDifficulty.ExpertPlus;
        const string expectedCharacteristic = "Standard";

        // Act
        var leaderboards = await _beatLeaderApi.GetLeaderboardsAsync(songHash);

        // Assert
        leaderboards.SuccessShould().NotBeNull("A valid song hash should return non-null leaderboards");
        leaderboards.SuccessShould()
            .Match<LeaderboardsResponse>(lb =>
                    lb.Leaderboards.Any(l =>
                        l.Difficulty.ModeName == expectedCharacteristic &&
                        l.Difficulty.DifficultyName == expectedDifficulty.ToString()),
                "Leaderboards should contain an entry for the expected difficulty and characteristic"
            );
    }

    [Test]
    public async Task GetLeaderboard_ShouldReturnNull_WhenInvalidLeaderboardId()
    {
        // Arrange
        var leaderboardId = _invalidLeaderboardId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 2,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };

        // Act
        var leaderboard = await _beatLeaderApi.GetLeaderboardAsync(leaderboardId, requestOptions);

        // Assert
        leaderboard.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetLeaderboard_ShouldReturnValidLeaderboard_WhenValidLeaderboardId()
    {
        // Arrange
        var leaderboardId = _validLeaderboardId;
        const EDifficulty expectedDifficulty = EDifficulty.ExpertPlus;
        const string expectedCharacteristic = "Standard";
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 2,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };

        // Act
        var leaderboard = await _beatLeaderApi.GetLeaderboardAsync(leaderboardId, requestOptions);

        // Assert
        leaderboard.SuccessShould()
            .NotBeNull("The leaderboard response should not be null")
            .And
            .Match<LeaderboardResponse>(x =>
                    x.Difficulty!.ModeName == expectedCharacteristic &&
                    x.Difficulty.DifficultyName == expectedDifficulty.ToString() &&
                    x.Scores!.Count > 0,
                "The leaderboard should have the expected difficulty, characteristic, and contain scores"
            );
    }

    [Test]
    public async Task GetLeaderboardAsyncEnumerable_ShouldReturnNullAndStopIteration_WhenInvalidLeaderboardId()
    {
        // Arrange
        var leaderboardId = _invalidLeaderboardId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in _beatLeaderApi.GetLeaderboardAsyncEnumerable(leaderboardId, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var leaderboard))
                Assert.Fail(data.Error);

            leaderboard.Should().BeNull("because the leaderboard ID is invalid");
        }
    }

    [Test]
    public async Task GetLeaderboardAsyncEnumerable_ShouldReturnValidLeaderboard_WhenValidLeaderboardId()
    {
        // Arrange
        var leaderboardId = _validLeaderboardId;
        const EDifficulty expectedDifficulty = EDifficulty.ExpertPlus;
        const string expectedCharacteristic = "Standard";
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };

        var leaderboards = new List<LeaderboardResponse>();

        // Act
        await foreach (var data in _beatLeaderApi.GetLeaderboardAsyncEnumerable(leaderboardId, requestOptions))
        {
            if (!data.TryGetValue(out var leaderboard))
                Assert.Fail(data.Error);

            if (leaderboard != null)
                leaderboards.Add(leaderboard);
        }

        // Assert
        leaderboards.Should().NotBeNullOrEmpty("The leaderboard response should not be null or empty");
        leaderboards.Should().AllSatisfy(x =>
        {
            x.Difficulty.Should().NotBeNull();
            x.Difficulty!.ModeName.Should().Be(expectedCharacteristic);
            x.Difficulty.DifficultyName.Should().Be(expectedDifficulty.ToString());
            x.Scores.Should().NotBeNullOrEmpty();
        }, "The leaderboard should have the expected difficulty, characteristic, and contain scores");
    }

    [Test]
    public async Task GetLeaderboardAsyncEnumerable_ShouldStopIteration_WhenNoMoreData()
    {
        // Arrange
        var leaderboardId = _validLeaderboardId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };

        // Act
        await foreach (var data in _beatLeaderApi.GetLeaderboardAsyncEnumerable(leaderboardId, requestOptions))
        {
            if (!data.TryGetValue(out var leaderboard))
                Assert.Fail(data.Error);

            leaderboard.Should().NotBeNull("because we expect to receive leaderboard data");
        }
    }

    [Test]
    public async Task GetLeaderboardAsyncEnumerable_ShouldStopIterationByReturningNull_WhenNoMoreScoresAvailable()
    {
        // Arrange
        var leaderboardId = _validLeaderboardId;
        var requestOptions = new BeatLeaderApi.PaginatedRequestOptions<LeaderboardSortBy>
        {
            Page = 1,
            PageSize = 100,
            MaxPage = int.MaxValue,
            Order = Order.Desc,
            SortBy = LeaderboardSortBy.Rank
        };

        var leaderboards = new List<LeaderboardResponse>();
        var receivedNull = false;

        // Act
        await foreach (var data in _beatLeaderApi.GetLeaderboardAsyncEnumerable(leaderboardId, requestOptions))
        {
            if (!data.TryGetValue(out var leaderboard))
                Assert.Fail(data.Error);

            if (leaderboard is null)
                receivedNull = true;
            else
                leaderboards.Add(leaderboard);
        }

        // Assert
        leaderboards.Should().NotBeEmpty("because we expect to receive at least some leaderboard data before stopping");
        receivedNull.Should()
            .BeTrue("because iteration should stop by receiving null when no more scores are available");
    }
}