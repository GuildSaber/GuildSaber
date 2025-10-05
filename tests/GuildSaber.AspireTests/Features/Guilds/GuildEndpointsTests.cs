//using Bogus;
//using static GuildSaber.Api.Features.Guilds.GuildRequests;
using System.Net.Http.Json;
using AwesomeAssertions;
using GuildSaber.Api.Features.Internal;
using GuildSaber.AspireTests.Data;
using static GuildSaber.Api.Features.Guilds.GuildResponses;

namespace GuildSaber.AspireTests.Features.Guilds;

public class GuildEndpointsTests
{
    /*
    private readonly Faker<CreateGuild> _createGuildFaker = new Faker<CreateGuild>()
        .CustomInstantiator(f => new CreateGuild(
            new CreateGuildInfo(
                f.Company.CompanyName(),
                f.Random.String2(2, 5).ToUpper(),
                f.Lorem.Sentence(),
                int.Parse(f.Internet.Color().Replace("#", ""), System.Globalization.NumberStyles.HexNumber)
            ),
            new CreateGuildRequirements(
                f.Random.Bool(),
                f.Random.Int(0, 5000),
                f.Random.Int(5001, 20000),
                f.Random.Int(0, 10000),
                f.Random.Int(10001, 20000),
                f.Random.Int(0, 365)
            )
        ));

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task CreateGuild_ShouldCreateGuild(HttpClientDataClass httpClientData)
    {
        // Arrange
        var createGuildRequest = _createGuildFaker.Generate();

        // Act
        var response = await httpClientData.HttpClient.PostAsJsonAsync("/guilds", createGuildRequest);
        var guildResponse = await response.Content.ReadFromJsonAsync<Guild>(httpClientData.JsonSerializerOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.Should().Be($"/guilds/{guildResponse!.Id}");

        guildResponse.Should().NotBeNull();
        guildResponse.Info.Name.Should().Be(createGuildRequest.Info.Name);
        guildResponse.Info.SmallName.Should().Be(createGuildRequest.Info.SmallName);
        guildResponse.Info.Description.Should().Be(createGuildRequest.Info.Description);
        guildResponse.Info.Color.Should().Be(createGuildRequest.Info.Color);
        guildResponse.Requirements.RequireSubmission.Should().Be(createGuildRequest.Requirements.RequireSubmission);
        guildResponse.Requirements.MinRank.Should().Be(createGuildRequest.Requirements.MinRank);
        guildResponse.Requirements.MaxRank.Should().Be(createGuildRequest.Requirements.MaxRank);
        guildResponse.Requirements.MinPP.Should().Be(createGuildRequest.Requirements.MinPP);
        guildResponse.Requirements.MaxPP.Should().Be(createGuildRequest.Requirements.MaxPP);
        guildResponse.Requirements.AccountAgeUnix.Should().Be(createGuildRequest.Requirements.AccountAgeUnix);
        guildResponse.Status.Should().Be(EGuildStatus.Unverified);
        guildResponse.Id.Should().BeGreaterThan(0);
        guildResponse.Info.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }*/

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task GetGuilds_ShouldReturnGuilds(HttpClientDataClass httpClientData)
    {
        // Arrange
        var httpClient = httpClientData.HttpClient;

        // Act
        var response = await httpClient.GetAsync("/guilds");
        var guilds = await response.Content.ReadFromJsonAsync<PagedList<Guild>>(httpClientData.JsonSerializerOptions);

        // Assert
        guilds.Data.Should().Contain(x => x.Info.SmallName == "CS");
    }
}