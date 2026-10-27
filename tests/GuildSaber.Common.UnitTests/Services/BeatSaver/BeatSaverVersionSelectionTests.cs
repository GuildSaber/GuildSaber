using AwesomeAssertions;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using Version = GuildSaber.Common.Services.BeatSaver.Models.Version;

namespace GuildSaber.Common.UnitTests.Services.BeatSaver;

public class BeatSaverVersionSelectionTests
{
    private static readonly SongHash _olderHash = SongHash.TryCreate(new string('a', 40)).Value;
    private static readonly SongHash _newerHash = SongHash.TryCreate(new string('b', 40)).Value;
    private static readonly DateTimeOffset _createdAt = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void LatestSelector_ShouldRespectBeatSaverOrderingRatherThanTimestamps()
    {
        var newer = CreateVersion(_newerHash, _createdAt.AddDays(1));
        var selected = CreateVersion(_olderHash, _createdAt);
        var beatMap = CreateBeatMap(newer, selected);

        BeatSaverApi.SelectLatestBeatMapVersion(beatMap).Should().BeSameAs(selected);
        beatMap.Versions.Should().Equal(newer, selected);
    }

    [Test]
    public void HashSelector_ShouldSelectExactVersionEvenWhenItIsNotCurrent()
    {
        var older = CreateVersion(_olderHash, _createdAt);
        var current = CreateVersion(_newerHash, _createdAt.AddDays(1));
        var beatMap = CreateBeatMap(older, current);

        BeatSaverApi.SelectBeatMapVersionByHash(_olderHash)(beatMap).Should().BeSameAs(older);
        BeatSaverApi.SelectLatestBeatMapVersion(beatMap).Should().BeSameAs(current);
    }

    [Test]
    public void HashSelector_ShouldIgnoreHashCasing()
    {
        var version = CreateVersion(_olderHash, _createdAt);
        var requestedHash = SongHash.TryCreate(_olderHash.ToString().ToUpperInvariant()).Value;

        BeatSaverApi.SelectBeatMapVersionByHash(requestedHash)(CreateBeatMap(version)).Should().BeSameAs(version);
    }

    [Test]
    public void HashSelector_ShouldNotFallBackToCurrentVersionWhenHashIsMissing()
    {
        var beatMap = CreateBeatMap(CreateVersion(_newerHash, _createdAt));

        BeatSaverApi.SelectBeatMapVersionByHash(_olderHash)(beatMap).Should().BeNull();
    }

    [Test]
    public void HashSelector_ShouldReturnNullWhenThereAreNoVersions()
        => BeatSaverApi.SelectBeatMapVersionByHash(_olderHash)(CreateBeatMap()).Should().BeNull();

    private static Version CreateVersion(SongHash hash, DateTimeOffset createdAt) => new()
    {
        Hash = hash,
        CreatedAt = createdAt,
        State = "Published",
        Diffs = [],
        DownloadURL = "",
        CoverURL = "",
        PreviewURL = ""
    };

    private static BeatMap CreateBeatMap(params Version[] versions) => new()
    {
        Id = BeatSaverKey.TryCreate("abc12").Value,
        Name = "Test map",
        Description = "Test description",
        Uploader = new Uploader { Id = 1, Name = "Mapper", Avatar = "" },
        Metadata = new Metadata
        {
            Bpm = 120, Duration = 60, SongName = "Test song", SongSubName = "",
            LevelAuthorName = "Mapper", SongAuthorName = "Artist"
        },
        Stats = new Stats { Plays = 0, Downloads = 0, Upvotes = 0, Downvotes = 0, Score = 0 },
        Uploaded = "2026-10-11T12:00:00Z",
        Automapper = false,
        Ranked = false,
        Qualified = false,
        Versions = [.. versions]
    };
}