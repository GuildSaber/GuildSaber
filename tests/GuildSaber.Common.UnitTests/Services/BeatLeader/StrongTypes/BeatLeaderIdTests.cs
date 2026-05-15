using AwesomeAssertions;
using GuildSaber.Common.Result;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;

namespace GuildSaber.Common.UnitTests.Services.BeatLeader.StrongTypes;

public class BeatLeaderIdTests
{
    private readonly BeatLeaderId _invalidBeatLeaderId = SteamId.CreateUnsafe(99999999999).Value;
    private readonly BeatLeaderId _validBeatLeaderId = BeatLeaderId.TryCreate(76561198126131670).Value;

    [Test]
    public async Task BeatLeaderId_CreateAsync_InvalidId_ReturnsNoneId()
        => (await BeatLeaderId.CreateAsync(_invalidBeatLeaderId, new HttpClient()))
            .Unwrap()
            .Should().HaveNoValue();

    [Test]
    public async Task BeatLeaderId_CreateAsync_ValidId_ReturnsSomeId()
        => (await BeatLeaderId.CreateAsync(_validBeatLeaderId, new HttpClient()))
            .Unwrap()
            .ValueShould().Be((BeatLeaderId)SteamId.CreateUnsafe(_validBeatLeaderId).Value);
}