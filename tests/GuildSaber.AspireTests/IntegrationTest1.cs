using GuildSaber.AspireTests.Data;

namespace GuildSaber.AspireTests;

public class IntegrationTest1
{
    [ClassDataSource<HttpClientDataClass>]
    [Test]
    public async Task GetWebResourceRootReturnsOkStatusCode(HttpClientDataClass httpClientData)
    {
        // Arrange
        var httpClient = httpClientData.HttpClient;

        // Act
        var response = await httpClient.GetAsync("/");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}