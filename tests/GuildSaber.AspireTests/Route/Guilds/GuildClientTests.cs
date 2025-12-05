using System.Linq.Expressions;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds;
using GuildSaber.Api.Features.Internal;
using GuildSaber.AspireTests.DataClasses;
using GuildSaber.AspireTests.Utils;
using GuildSaber.CSharpClient.Routes.Internal;
using static GuildSaber.Api.Features.Guilds.GuildResponses;

namespace GuildSaber.AspireTests.Route.Guilds;

[ClassDataSource<GuildSaberClientDataClass>]
public class GuildClientTests(GuildSaberClientDataClass dataClass)
{
    private const int ValidGuildId = 1;
    private const int InvalidGuildId = 999999;

    [Test]
    public async Task GetByIdAsync_ShouldReturnGuild_WhenGuildExists()
    {
        // Arrange
        var guildId = ValidGuildId;

        // Act
        var result = await dataClass.GuildSaberClient.Guilds.GetByIdAsync(guildId, CancellationToken.None);

        // Assert
        result.SuccessShould().NotBeNull().And.BeOfType<Guild>().Which.Id.Should().Be(1);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenGuildDoesNotExist()
    {
        // Arrange
        var guildId = InvalidGuildId;

        // Act
        var result = await dataClass.GuildSaberClient.Guilds.GetByIdAsync(guildId, CancellationToken.None);

        // Assert
        result.SuccessShould().BeNull();
    }

    [Test]
    public async Task GetAsync_ShouldReturnPagedListOfGuilds_WhenCalledWithValidParameters()
    {
        // Arrange
        string? search = null;
        var requestOptions = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = 1,
            PageSize = 5,
            MaxPage = 2,
            Order = EOrder.Desc,
            SortBy = GuildRequests.EGuildSorter.Name
        };

        // Act
        var result = await dataClass.GuildSaberClient.Guilds.GetAsync(search, requestOptions, CancellationToken.None);

        // Assert
        result.SuccessShould().NotBeNull().And.BeOfType<PagedList<Guild>>()
            .Which.Data.Should().NotBeEmpty("because there should be guilds in the first page");
    }

    [Test]
    public async Task GetAsync_ShouldReturnEmptyPagedList_WhenNoGuildsMatchSearch()
    {
        // Arrange
        var search = "ThisSearchTermShouldNeverMatchAnyGuild12345";
        var requestOptions = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = 1,
            PageSize = 5,
            MaxPage = 2,
            Order = EOrder.Desc,
            SortBy = GuildRequests.EGuildSorter.Name
        };

        // Act
        var result = await dataClass.GuildSaberClient.Guilds.GetAsync(search, requestOptions, CancellationToken.None);

        // Assert
        result.SuccessShould().NotBeNull().And.BeOfType<PagedList<Guild>>()
            .Which.Data.Should().BeEmpty("because no guilds should match the search term");
    }

    [Test]
    public async Task GetAsyncEnumerable_ShouldReturnEmptyAndStopIteration_WhenNoGuildsMatchSearch()
    {
        // Arrange
        var search = "ThisSearchTermShouldNeverMatchAnyGuild12345";
        var requestOptions = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = 1,
            PageSize = 1,
            MaxPage = 2,
            Order = EOrder.Desc,
            SortBy = GuildRequests.EGuildSorter.Name
        };
        var iterationCount = 0;

        // Act
        await foreach (var data in dataClass.GuildSaberClient.Guilds.GetAsyncEnumerable(search, requestOptions))
        {
            iterationCount++.Should().Be(0, "because we expect to stop after the first iteration");

            if (!data.TryGetValue(out var guilds))
                Assert.Fail(data.Error);

            guilds.Should().BeEmpty("because no guilds match the search term");
        }
    }

    [Test]
    [Arguments(GuildRequests.EGuildSorter.Name, EOrder.Desc)]
    [Arguments(GuildRequests.EGuildSorter.CreationDate, EOrder.Asc)]
    [Arguments(GuildRequests.EGuildSorter.Name, EOrder.Asc)]
    [Arguments(GuildRequests.EGuildSorter.CreationDate, EOrder.Desc)]
    public async Task GetAsyncEnumerable_ShouldReturnGuildsInCorrectSortingOrder_WhenOrderByAndSortBySpecified(
        GuildRequests.EGuildSorter sortBy, EOrder orderBy)
    {
        // Arrange
        string? search = null;
        var requestOptions = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = 1,
            PageSize = 1,
            MaxPage = 2,
            SortBy = sortBy,
            Order = orderBy
        };

        var guilds = new List<Guild>();

        // Act
        await foreach (var data in dataClass.GuildSaberClient.Guilds.GetAsyncEnumerable(search, requestOptions))
        {
            if (!data.TryGetValue(out var guildResponses))
                Assert.Fail(data.Error);

            guilds.AddRange(guildResponses!);
        }

        // Arrange Assertion
        var compFunc = (Expression<Func<Guild, IComparable>>)(sortBy switch
        {
            GuildRequests.EGuildSorter.Name => x => x.Info.Name,
            GuildRequests.EGuildSorter.CreationDate => x => x.Info.CreatedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        });

        // Assert
        guilds.Should()
            .BeInOrders(compFunc, orderBy, "because the guilds should be sorted according to the specified sortBy")
            .And.HaveCount((requestOptions.MaxPage - requestOptions.Page + 1) * requestOptions.PageSize);
    }

    [Test]
    public async Task GetAsyncEnumerable_ShouldReturnEmptyArray_WhenNoMoreData()
    {
        // Arrange
        string? search = null;
        var requestOptions = new PaginatedRequestOptions<GuildRequests.EGuildSorter>
        {
            Page = 1,
            PageSize = 2,
            MaxPage = 2,
            Order = EOrder.Desc,
            SortBy = GuildRequests.EGuildSorter.Name
        };

        Guild[]? lastGuilds = [];
        // Act
        await foreach (var data in dataClass.GuildSaberClient.Guilds.GetAsyncEnumerable(search, requestOptions))
        {
            if (!data.TryGetValue(out var guilds))
                Assert.Fail(data.Error);

            lastGuilds = guilds;
        }

        // Assert
        lastGuilds.Should().NotBeNull().And.BeEmpty("because there should be no more data after the last page");
    }

    /*[Test]
    public async Task SetDiscordGuildIdAsync_ShouldUpdateDiscordGuildId_WhenGuildExists()
    {
        // Arrange
        var guildId = ValidGuildId;
        ulong? discordGuildId = 976932780971798579;

        // Act
        var result =
            await dataClass.GuildSaberClient.Guilds.SetDiscordGuildIdAsync(guildId, discordGuildId,
                CancellationToken.None);

        // Assert
        result.SuccessShould().NotBeNull().And.BeOfType<Guild>()
            .Which.DiscordInfo.MainDiscordGuildId.Should().Be(discordGuildId.ToString());
    }

    [Test]
    public async Task SetDiscordGuildIdAsync_ShouldSetDiscordGuildIdToNull_WhenProvidedNullValue()
    {
        // Arrange
        var guildId = ValidGuildId;
        ulong? discordGuildId = null;

        // Act
        var result =
            await dataClass.GuildSaberClient.Guilds.SetDiscordGuildIdAsync(guildId, discordGuildId,
                CancellationToken.None);

        // Assert
        result.SuccessShould().NotBeNull().And.BeOfType<Guild>()
            .Which.DiscordInfo.MainDiscordGuildId.Should().BeNull();
    }

    [Test]
    public async Task SetDiscordGuildIdAsync_ShouldUpdateDiscordGuildIdTwice_WhenCalledConsecutively()
    {
        // Arrange
        var guildId = ValidGuildId;
        ulong? validDiscordGuildId = 976932780971798579;

        // Act - First update to null
        var firstResult =
            await dataClass.GuildSaberClient.Guilds.SetDiscordGuildIdAsync(guildId, null, CancellationToken.None);

        // Assert - First update
        firstResult.SuccessShould().NotBeNull().And.BeOfType<Guild>()
            .Which.DiscordInfo.MainDiscordGuildId.Should().BeNull();

        // Act - Second update back to valid value
        var secondResult =
            await dataClass.GuildSaberClient.Guilds.SetDiscordGuildIdAsync(guildId, validDiscordGuildId,
                CancellationToken.None);

        // Assert - Second update
        secondResult.SuccessShould().NotBeNull().And.BeOfType<Guild>()
            .Which.DiscordInfo.MainDiscordGuildId.Should().Be(validDiscordGuildId.ToString());
    }

    [Test]
    public async Task SetDiscordGuildIdAsync_ShouldReturnFailure_WhenGuildDoesNotExist()
    {
        // Arrange
        var guildId = InvalidGuildId;
        ulong? discordGuildId = 976932780971798579;

        // Act
        var result =
            await dataClass.GuildSaberClient.Guilds.SetDiscordGuildIdAsync(guildId, discordGuildId,
                CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"Guild with ID {guildId} not found");
    }*/
}