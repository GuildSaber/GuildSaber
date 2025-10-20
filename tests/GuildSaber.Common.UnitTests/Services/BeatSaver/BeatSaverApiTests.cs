using AwesomeAssertions;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;

namespace GuildSaber.Common.UnitTests.Services.BeatSaver;

public class BeatSaverApiTests
{
    private readonly BeatSaverApi _beatSaverApi;

    private readonly BeatSaverKey _invalidBeatSaverKey = BeatSaverKey
        .CreateUnsafe("0").Value;

    private readonly SongHash _invalidSongHash = SongHash
        .CreateUnsafe("abcdef1234567890abcdef1234567890abcdef12").Value;

    private readonly BeatSaverKey _validBeatSaverKey = BeatSaverKey
        .CreateUnsafe("a3c3").Value;

    private readonly SongHash _validSongHash = SongHash
        .CreateUnsafe("c4ccc41a43bb15f252b025f03bce6f9c1dbbdbeb").Value;

    public BeatSaverApiTests()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.beatsaver.com/");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GuildSaber");

        _beatSaverApi = new BeatSaverApi(httpClient);
    }

    [Test]
    public async Task GetBeatMap_ByHash_ShouldReturnNull_WhenInvalidSongHash()
    {
        // Arrange
        var songHash = _invalidSongHash;

        // Act
        var result = await _beatSaverApi.GetBeatMapAsync(songHash);

        // Assert
        result.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetBeatMap_ByHash_ShouldReturnBeatMap_WhenValidSongHash()
    {
        // Arrange
        var songHash = _validSongHash;

        // Act
        var result = await _beatSaverApi.GetBeatMapAsync(songHash);

        // Assert
        result.SuccessShould().NotBeNull("A valid song hash should return a BeatMap");
    }

    [Test]
    public async Task GetBeatMap_ByKey_ShouldReturnNull_WhenInvalidBeatSaverKey()
    {
        // Arrange
        var beatSaverKey = _invalidBeatSaverKey;

        // Act
        var result = await _beatSaverApi.GetBeatMapAsync(beatSaverKey);

        // Assert
        result.SuccessShould().BeNull("A 404 response should return a null success response");
    }

    [Test]
    public async Task GetBeatMap_ByKey_ShouldReturnBeatMap_WhenValidBeatSaverKey()
    {
        // Arrange
        var beatSaverKey = _validBeatSaverKey;

        // Act
        var result = await _beatSaverApi.GetBeatMapAsync(beatSaverKey);

        // Assert
        result.SuccessShould().NotBeNull("A valid BeatSaver key should return a BeatMap");
    }
}