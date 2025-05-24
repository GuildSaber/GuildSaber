using FluentAssertions;
using GuildSaber.Common.Result;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using Xunit.Abstractions;

namespace GuildSaber.UnitTests.Database.Models.StrongTypes;

public class BeatLeaderIdTests(ITestOutputHelper testOutputHelper)
{
    private readonly BeatLeaderId _invalidBeatLeaderId = BeatLeaderId.CreateUnsafe(99999999999).Value;
    private readonly BeatLeaderId _validBeatLeaderId = BeatLeaderId.CreateUnsafe(76561198126131670).Value;

    [Fact]
    public async Task BeatLeaderId_CreateAsync_InvalidId_ReturnsNoneId()
        => (await BeatLeaderId.CreateAsync(_invalidBeatLeaderId, new HttpClient()))
            .Unwrap()
            .Should().HaveNoValue();

    [Fact]
    public async Task BeatLeaderId_CreateAsync_ValidId_ReturnsSomeId()
        => (await BeatLeaderId.CreateAsync(_validBeatLeaderId, new HttpClient()))
            .Unwrap()
            .ValueShould().Be(BeatLeaderId.CreateUnsafe(_validBeatLeaderId).Value);
}