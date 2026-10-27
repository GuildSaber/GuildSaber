using System.Linq.Expressions;
using AwesomeAssertions;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;

namespace GuildSaber.Common.UnitTests.Services.ScoreSaber;

public class ScoreSaberApiTests
{
    private readonly SSLeaderboardId _invalidLeaderboardId = SSLeaderboardId.TryCreate(int.MaxValue).Value;
    private readonly ScoreSaberId _invalidScoreSaberId = ScoreSaberId.CreateUnsafe(99999999999).Value;

    private readonly string _invalidSongCharacteristic = "InvalidMode";
    private readonly SongHash _invalidSongHash = SongHash.TryCreate("abcdef1234567890abcdef1234567890abcdef12").Value;
    private readonly ScoreSaberApi _scoreSaberApi;
    private readonly SSLeaderboardId _validLeaderboardId = SSLeaderboardId.TryCreate(236516).Value;
    private readonly ScoreSaberId _validScoreSaberId = ScoreSaberId.CreateUnsafe(76561198126131670).Value;

    private readonly string _validSongCharacteristic = "Standard";
    private readonly EDifficulty _validSongDifficulty = EDifficulty.ExpertPlus;
    private readonly SongHash _validSongHash = SongHash.TryCreate("c4ccc41a43bb15f252b025f03bce6f9c1dbbdbeb").Value;

    public ScoreSaberApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://scoresaber.com/");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _scoreSaberApi = new ScoreSaberApi(httpClient);
    }

    [Test]
    public async Task GetPlayerScores_ShouldReturnNullAndStopIteration_WhenInvalidPlayerId()
    {
        // Arrange
        var playerId = _invalidScoreSaberId;
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<PlayerScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            SortBy = PlayerScoresSortBy.Recent
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in _scoreSaberApi.GetPlayerScores(playerId, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().BeNull("because the player ID is invalid");
        }
    }

    [Test]
    [Arguments(PlayerScoresSortBy.Recent), Arguments(PlayerScoresSortBy.Top)]
    [Arguments(PlayerScoresSortBy.Oldest), Arguments(PlayerScoresSortBy.Accuracy)]
    public async Task GetPlayerScores_ShouldReturnScoresInCorrectSortingOrder_WhenSortBySpecified(
        PlayerScoresSortBy sortBy)
    {
        // Arrange
        var playerId = _validScoreSaberId;
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<PlayerScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            SortBy = sortBy
        };

        var scores = new List<PlayerScore>();

        // Act
        await foreach (var data in _scoreSaberApi.GetPlayerScores(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var playerScores))
                Assert.Fail(data.Error);

            scores.AddRange(playerScores!);
        }

        // Arrange Assertion
        var compFunc = (Expression<Func<PlayerScore, IComparable>>)(sortBy switch
        {
            PlayerScoresSortBy.Recent => x => x.Score.CreatedAt,
            PlayerScoresSortBy.Top => x => x.Score.Pp,
            PlayerScoresSortBy.Oldest => x => x.Score.CreatedAt,
            PlayerScoresSortBy.Accuracy => x => x.Score.Accuracy,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });


        // Assert
        if (sortBy == PlayerScoresSortBy.Oldest)
            scores.Should().BeInAscendingOrder(compFunc,
                "because the scores should be sorted according to the specified sortBy");
        else
            scores.Should().BeInDescendingOrder(compFunc,
                "because the scores should be sorted according to the specified sortBy");

        scores.Should().HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
        scores.Should().OnlyContain(x => x.Score.PersonalBest);
    }

    [Test]
    public async Task GetPlayerScores_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        var playerId = _validScoreSaberId;
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<PlayerScoresSortBy>
        {
            Page = 10_000,
            PageSize = 2,
            MaxPage = 10_002,
            SortBy = PlayerScoresSortBy.Recent
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in _scoreSaberApi.GetPlayerScores(playerId, requestOptions))
        {
            iterationCount++;
            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().NotBeNull().And.BeEmpty("because the requested page has no scores");
        }

        iterationCount.Should().Be(1, "because enumeration should stop after the empty page");
    }

    [Test]
    [Arguments(LeaderboardScoresSortBy.Score, SortDirection.Desc)]
    [Arguments(LeaderboardScoresSortBy.Score, SortDirection.Asc)]
    [Arguments(LeaderboardScoresSortBy.TimeSet, SortDirection.Desc)]
    [Arguments(LeaderboardScoresSortBy.TimeSet, SortDirection.Asc)]
    public async Task GetLeaderboardScores_ShouldReturnScoresInCorrectSortingOrder_WhenSortBySpecified(
        LeaderboardScoresSortBy sortBy, SortDirection sortDirection)
    {
        // Arrange
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<LeaderboardScoresSortBy>
        {
            Page = 1, PageSize = 2, MaxPage = 2, SortBy = sortBy, SortDirection = sortDirection
        };
        var scores = new List<Score>();

        // Act
        await foreach (var data in _scoreSaberApi.GetLeaderboardScores(_validLeaderboardId, requestOptions))
        {
            if (!data.TryGetValue(out var page))
                Assert.Fail(data.Error);

            scores.AddRange(page!);
        }

        // Assert
        var compFunc = (Expression<Func<Score, IComparable>>)(sortBy switch
        {
            LeaderboardScoresSortBy.Score => x => x.ModifiedScore,
            LeaderboardScoresSortBy.TimeSet => x => x.CreatedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });
        if (sortDirection == SortDirection.Asc)
            scores.Should().BeInAscendingOrder(compFunc);
        else
            scores.Should().BeInDescendingOrder(compFunc);

        scores.Should().HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Test]
    public async Task GetLeaderboardScores_ShouldReturnEmptyArrayAndStopIteration_WhenInvalidLeaderboardId()
    {
        // Arrange
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<LeaderboardScoresSortBy>
        {
            Page = 1, PageSize = 2, MaxPage = 2
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in _scoreSaberApi.GetLeaderboardScores(_invalidLeaderboardId, requestOptions))
        {
            iterationCount++;
            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().NotBeNull().And.BeEmpty("because the leaderboard ID is invalid");
        }

        // Assert
        iterationCount.Should().Be(1, "because enumeration should stop after the empty page");
    }

    [Test]
    public async Task GetLeaderboardInfo_ShouldReturnNull_WhenInvalidSongDifficultyOrGameMode()
    {
        // Arrange
        var songHash = _invalidSongHash;
        var characteristic = SSGameMode.TryCreate(_invalidSongCharacteristic).Value;

        // Act
        var leaderboardInfo =
            await _scoreSaberApi.GetLeaderboardInfoAsync(songHash, EDifficulty.ExpertPlus, characteristic);

        // Assert
        leaderboardInfo.SuccessShould().BeNull();
    }

    [Test]
    public async Task GetLeaderboardInfo_ShouldReturnValidLeaderboard_WhenValidSongData()
    {
        // Arrange
        var songHash = _validSongHash;
        var difficulty = _validSongDifficulty;
        var characteristic = SSGameMode.TryCreate(_validSongCharacteristic).Value;

        // Act
        var leaderboardInfo = await _scoreSaberApi.GetLeaderboardInfoAsync(songHash, difficulty, characteristic);

        // Assert
        leaderboardInfo.SuccessShould().NotBeNull();
        leaderboardInfo.SuccessShould()
            .Match<Leaderboard>(lb =>
                    lb.Difficulty.EDifficulty == difficulty &&
                    lb.Difficulty.GameMode == characteristic,
                "Leaderboard should match the requested difficulty and characteristic"
            );
    }
}