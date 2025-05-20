using CSharpFunctionalExtensions;
using FluentAssertions;
using GuildSaber.Common.Result;
using GuildSaber.Database.Models.Server.StrongTypes;
using Xunit.Abstractions;

namespace GuildSaber.UnitTests.Database.Models.StrongTypes;

public class BeatLeaderIdTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task BeatLeaderId_CreateAsync_InValidId_ReturnsNoneId()
        => (await BeatLeaderId.CreateAsync(99999999999, new HttpClient()))
            .Unwrap()
            .Should().Be(Maybe<BeatLeaderId>.None);

    [Fact]
    public async Task BeatLeaderId_CreateAsync_ValidId_ReturnsSomeId()
        => (await BeatLeaderId.CreateAsync(76561198126131670, new HttpClient()))
            .Unwrap()
            .Should().Be(BeatLeaderId.CreateUnsafe(76561198126131670));
}