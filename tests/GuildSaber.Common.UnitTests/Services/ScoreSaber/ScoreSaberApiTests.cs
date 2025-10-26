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
    private readonly ScoreSaberId _invalidScoreSaberId = ScoreSaberId.CreateUnsafe(99999999999).Value;

    private readonly string _invalidSongCharacteristic = "InvalidMode";
    private readonly EDifficulty _invalidSongDifficulty = (EDifficulty)(-1);
    private readonly SongHash _invalidSongHash = SongHash.TryCreate("abcdef1234567890abcdef1234567890abcdef12").Value;
    private readonly ScoreSaberApi _scoreSaberApi;
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
            PlayerScoresSortBy.Recent => x => x.Score.TimeSet,
            PlayerScoresSortBy.Top => x => x.Score.Pp,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });


        // Assert
        scores.Should()
            .BeInDescendingOrder(compFunc, "because the scores should be sorted according to the specified sortBy")
            .And.HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Test]
    public async Task GetPlayerScores_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        var playerId = _validScoreSaberId;
        var requestOptions = new ScoreSaberApi.PaginatedRequestOptions<PlayerScoresSortBy>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            SortBy = PlayerScoresSortBy.Recent
        };

        // Act
        await foreach (var data in _scoreSaberApi.GetPlayerScores(playerId, requestOptions))
        {
            if (!data.TryGetValue(out var scores))
                Assert.Fail(data.Error);

            scores.Should().NotBeNullOrEmpty("because we expect to receive scores");
        }
    }

    [Test]
    public async Task GetLeaderboardInfo_ShouldReturnNull_WhenInvalidSongDifficultyOrGameMode()
    {
        // Arrange
        var songHash = _invalidSongHash;
        var difficulty = _invalidSongDifficulty;
        var characteristic = SSGameMode.TryCreate(_invalidSongCharacteristic).Value;

        // Act
        var leaderboardInfo = await _scoreSaberApi.GetLeaderboardInfoAsync(songHash, difficulty, characteristic);

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
            .Match<LeaderboardInfo>(lb =>
                    lb.Difficulty.Difficulty == difficulty &&
                    lb.Difficulty.GameMode == characteristic,
                "Leaderboard should match the requested difficulty and characteristic"
            );
    }
}