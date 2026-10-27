using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Songs;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.Services.ScoreSaber.Models.Responses;
using GuildSaber.Common.Services.ScoreSaber.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Color = System.Drawing.Color;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;
using DraftResponse = GuildSaber.Api.Features.MapRequests.Services.MapProposalDraftService.CreateResponse;
using BLResponses = GuildSaber.Common.Services.BeatLeader.Models.Responses;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;
using Player = GuildSaber.Database.Models.Server.Players.Player;
using Version = GuildSaber.Common.Services.BeatSaver.Models.Version;

namespace GuildSaber.Common.UnitTests.Features.Songs;

public class SongDifficultyResolutionTests
{
    private static readonly SongHash _hash = SongHash.TryCreate(new string('a', 40)).Value;
    private static readonly SSLeaderboardId _leaderboardId = SSLeaderboardId.TryCreate(123).Value;
    private static readonly DateTimeOffset _createdAt = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private sealed class ResponseHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
    }

    private sealed class TestDatabase(DbContextOptions<ServerDbContext> options) : IAsyncDisposable
    {
        public ServerDbContext Context { get; } = new(options);

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Context.Database.EnsureDeletedAsync();
            }
            finally
            {
                await Context.DisposeAsync();
            }
        }

        public ServerDbContext CreateContext() => new(options);

        public static async Task<TestDatabase> CreateAsync(bool seedDifficulties = true)
        {
            var connectionString = Environment.GetEnvironmentVariable("GUILDSABER_TEST_POSTGRES");
            Skip.Unless(!string.IsNullOrWhiteSpace(connectionString),
                "Set GUILDSABER_TEST_POSTGRES to an isolated PostgreSQL server to run persistence tests.");
            var connection = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = $"guildsaber_song_test_{Guid.NewGuid():N}", Pooling = false
            };
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            optionsBuilder.WithExpressionExpanding();
            var database = new TestDatabase(optionsBuilder.Options);
            try
            {
                await database.Context.Database.EnsureCreatedAsync();
                database.Context.GameModes.AddRange(
                    new GameMode { Id = new GameMode.GameModeId(1), Name = "Standard" },
                    new GameMode { Id = new GameMode.GameModeId(2), Name = "OneSaber" });
                if (seedDifficulties)
                    database.Context.AddRange(
                        new Song
                        {
                            Id = new Song.SongId(1), Hash = _hash, UploadedAt = _createdAt,
                            Info = new SongInfo("Test", "Test", "", "Artist", "Mapper")
                        },
                        new Song
                        {
                            Id = new Song.SongId(2), Hash = SongHash.TryCreate(new string('b', 40)).Value,
                            UploadedAt = _createdAt,
                            Info = new SongInfo("Other version", "Other version", "", "Artist", "Mapper")
                        },
                        CreateDifficulty(1, 1, 1, EDifficulty.Expert), CreateDifficulty(2, 1, 1, EDifficulty.Hard),
                        CreateDifficulty(3, 1, 2, EDifficulty.Expert), CreateDifficulty(4, 2, 1, EDifficulty.Expert));
                await database.Context.SaveChangesAsync();
                database.Context.ChangeTracker.Clear();
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }
    }

    [Test]
    public async Task CreateDraft_ShouldResolveKeyAndHistoricalHashAndKeepProposalsInTheSameContext()
    {
        await using var database = await TestDatabase.CreateAsync(seedDifficulties: false);
        var db = database.Context;
        await SeedRequestParentsAsync(db);
        var historical = CreateBeatMap().Versions[0];
        historical.Hash = SongHash.TryCreate(new string('b', 40)).Value;
        historical.CreatedAt = _createdAt.AddDays(1); // Array order, not date, determines BeatSaver's current version.
        var beatMap = CreateBeatMap();
        beatMap.Versions.Insert(0, historical);
        var lightshow = CreateBeatMap().Versions[0].Diffs[0];
        lightshow.Difficulty = "Easy";
        lightshow.MaxScore = 0;
        beatMap.Versions[^1].Diffs.Add(lightshow);
        using var beatSaverClient = CreateClient(HttpStatusCode.OK, JsonSerializer.Serialize(beatMap));
        using var beatLeaderClient =
            CreateClient(HttpStatusCode.OK, JsonSerializer.Serialize(CreateBeatLeaderResponse()));
        using var scoreSaberClient = CreateClient(HttpStatusCode.NotFound);
        var service = new MapProposalDraftService(db, new BeatSaverApi(beatSaverClient),
            new SongService(db, new BeatLeaderApi(beatLeaderClient), new ScoreSaberApi(scoreSaberClient)),
            TimeProvider.System, Options.Create(new MapRequestSettings()));
        var request = CreateDraftRequest(
            new MapProposalRequests.MapVersionInput(new MapProposalRequests.SongReference.ByKey(beatMap.Id), "Standard",
                EDifficulty.Expert, "Standard", 1),
            new MapProposalRequests.MapVersionInput(new MapProposalRequests.SongReference.ByHash(historical.Hash),
                "Standard", EDifficulty.Expert,
                "Standard", 0));

        var draft =
            (await service.CreateAsync(new ContextId(1), null, new PlayerId(1), request, CancellationToken.None))
            .Should().BeOfType<DraftResponse.Success>().Which.Proposal;

        draft.Id.Value.Should().BeGreaterThan(0);
        draft.ThreadId.Value.Should().BeGreaterThan(0);
        draft.Rating.AccStar.Should().BeNull();
        draft.Rating.DiffStar!.Value.Value.Should().Be(5);
        draft.Rating.IsAccStarAutoCalculated.Should().BeFalse();
        draft.MapVersions.Should().HaveCount(2).And.OnlyContain(x => x.SongDifficultyId.Value > 0);
        draft.MapVersions.Single(x => x.Order == 0).PlayModeName.Should().Be("Standard");
        draft.Categories.Single().Name.Should().Be("Category");
        draft.Achievements.Single().Name.Should().Be("Achievement");
        var hashes = await db.MapProposalVersions.Where(x => x.ProposalId == draft.Id).OrderBy(x => x.Order)
            .Select(x => db.Songs.Single(song => song.Id == x.SongDifficulty.SongId).Hash).ToArrayAsync();
        hashes.Should().Equal(historical.Hash, _hash);

        (await service.CreateAsync(new ContextId(2), draft.ThreadId, new PlayerId(1), request, CancellationToken.None))
            .Should().BeOfType<DraftResponse.ThreadNotFound>();
        (await service.CreateAsync(new ContextId(1), draft.ThreadId, new PlayerId(1), request, CancellationToken.None))
            .Should().BeOfType<DraftResponse.Success>();
        (await db.MapRequestThreads.CountAsync()).Should().Be(1);
        (await db.MapProposals.CountAsync()).Should().Be(2);
        (await db.SongDifficulties.CountAsync()).Should().Be(2);
        (await db.RankedMaps.AnyAsync()).Should().BeFalse();
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task FailedDraft_ShouldDiscardImportsOrBackfills(
        bool ambiguousRatingSource, bool existingDifficulty)
    {
        await using var database = await TestDatabase.CreateAsync(seedDifficulties: existingDifficulty);
        var db = database.Context;
        await SeedRequestParentsAsync(db);
        var beatMap = CreateBeatMap();
        var otherHash = SongHash.TryCreate(new string('c', 40)).Value;
        if (ambiguousRatingSource)
        {
            var other = CreateBeatMap().Versions[0];
            other.Hash = otherHash;
            beatMap.Versions.Insert(0, other);
        }

        using var beatSaverClient = CreateClient(HttpStatusCode.OK, JsonSerializer.Serialize(beatMap));
        using var beatLeaderClient =
            CreateClient(HttpStatusCode.OK, JsonSerializer.Serialize(CreateBeatLeaderResponse()));
        using var scoreSaberClient = CreateClient(HttpStatusCode.NotFound);
        var service = new MapProposalDraftService(db, new BeatSaverApi(beatSaverClient),
            new SongService(db, new BeatLeaderApi(beatLeaderClient), new ScoreSaberApi(scoreSaberClient)),
            TimeProvider.System, Options.Create(new MapRequestSettings()));
        var request = CreateDraftRequest(
            new MapProposalRequests.MapVersionInput(new MapProposalRequests.SongReference.ByKey(beatMap.Id), "Standard",
                EDifficulty.Expert, "Standard", 0),
            new MapProposalRequests.MapVersionInput(new MapProposalRequests.SongReference.ByHash(otherHash), "Standard",
                EDifficulty.Expert, "Standard",
                0));

        var result =
            await service.CreateAsync(new ContextId(1), null, new PlayerId(1), request, CancellationToken.None);

        if (ambiguousRatingSource) result.Should().BeOfType<DraftResponse.ValidationFailure>();
        else result.Should().BeOfType<DraftResponse.ResolutionFailure>();
        db.ChangeTracker.HasChanges().Should().BeFalse();
        await db.SaveChangesAsync(); // No failed import or backfill may leak into a later save.
        (await db.MapRequestThreads.AnyAsync()).Should().BeFalse();
        (await db.MapProposals.AnyAsync()).Should().BeFalse();
        (await db.Songs.CountAsync()).Should().Be(existingDifficulty ? 2 : 0);
        (await db.SongDifficulties.AnyAsync(x => x.BLLeaderboardId != null)).Should().BeFalse();
    }

    private static MapProposalRequests.CreateDraft CreateDraftRequest(
        params MapProposalRequests.MapVersionInput[] versions)
        => new(versions, new RankedMapRequests.RankedMapRequirements(),
            new RankedMapRequests.ManualRating(DifficultyStar: 5), [new CategoryId(1)], [1]);

    private static async Task SeedRequestParentsAsync(ServerDbContext db)
    {
        db.AddRange(
            new Guild
            {
                Id = new GuildId(1),
                Info = GuildInfo.TryCreate("Test Guild", "TEST", "Test guild", Color.White, _createdAt).Value,
                Requirements = default, Status = Guild.EGuildStatus.Unverified, DiscordInfo = default
            },
            new Context { Id = new ContextId(1), GuildId = new GuildId(1), Info = new ContextInfo("Main", "") },
            new Context { Id = new ContextId(2), GuildId = new GuildId(1), Info = new ContextInfo("Other", "") },
            new Player
            {
                Id = new PlayerId(1), Info = new PlayerInfo("Author", "", "", _createdAt), Platform = default,
                LinkedAccounts = default, SubscriptionInfo = default
            },
            new PlayMode { Id = new PlayMode.PlayModeId(1), Name = "Standard" },
            new Category
            {
                Id = new CategoryId(1), GuildId = new GuildId(1),
                Info = CategoryInfo.TryCreate("Category", "Test category").Value
            },
            new RankedMapListAchievement(new Achievement.AchievementId(1), new GuildId(1), new ContextId(1), null,
                new AchievementInfo("Achievement", Color.White), default, null, false, default, 1));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    [Test]
    [Arguments("SoloStandard", EDifficulty.Expert, 1)]
    [Arguments("solostandard", EDifficulty.Hard, 2)]
    [Arguments("OneSaber", EDifficulty.Expert, 3)]
    public async Task MissingScoreSaberId_ShouldMatchHashCharacteristicAndDifficulty(
        string gameMode, EDifficulty difficulty, long expectedId)
    {
        await using var database = await TestDatabase.CreateAsync();
        var leaderboard = CreateLeaderboard(_hash.ToString().ToUpperInvariant(), gameMode, difficulty);

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            leaderboard, database.Context, CancellationToken.None);

        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(new SongDifficultyId(expectedId));
        var resolved = await database.Context.SongDifficulties.Where(x => x.SSLeaderboardId != null).SingleAsync();
        resolved.Id.Should().Be(result.Value);
        resolved.SSLeaderboardId.Should().Be(_leaderboardId);
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Test]
    public async Task KnownScoreSaberId_ShouldUseExistingMapping()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Context.SongDifficulties.Where(x => x.Id == new SongDifficultyId(1))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SSLeaderboardId, _leaderboardId));

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(), database.Context, CancellationToken.None);

        result.Value.Should().Be(new SongDifficultyId(1));
        (await database.Context.SongDifficulties.CountAsync(x => x.SSLeaderboardId != null)).Should().Be(1);
    }

    [Test]
    [Arguments("_ostFoo", "SoloStandard", EDifficulty.Expert)]
    [Arguments("cccccccccccccccccccccccccccccccccccccccc", "SoloStandard", EDifficulty.Expert)]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Lawless", EDifficulty.Expert)]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "SoloStandard", EDifficulty.Easy)]
    public async Task UnmatchedScore_ShouldNotCreateOrChangeDifficulties(
        string hash, string gameMode, EDifficulty difficulty)
    {
        await using var database = await TestDatabase.CreateAsync();

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(hash, gameMode, difficulty), database.Context, CancellationToken.None);

        result.HasValue.Should().BeFalse();
        (await database.Context.SongDifficulties.CountAsync()).Should().Be(4);
        (await database.Context.SongDifficulties.AnyAsync(x => x.SSLeaderboardId != null)).Should().BeFalse();
    }

    [Test]
    public async Task ConflictingScoreSaberId_ShouldNotBeOverwritten()
    {
        await using var database = await TestDatabase.CreateAsync();
        var existingId = SSLeaderboardId.TryCreate(456).Value;
        await database.Context.SongDifficulties.Where(x => x.Id == new SongDifficultyId(1))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SSLeaderboardId, existingId));

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(), database.Context, CancellationToken.None);

        result.HasValue.Should().BeFalse();
        (await database.Context.SongDifficulties.SingleAsync(x => x.Id == new SongDifficultyId(1)))
            .SSLeaderboardId.Should().Be(existingId);
    }

    [Test]
    public async Task AmbiguousHashCasing_ShouldNotChooseAnArbitraryDifficulty()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.AddRange(
            new Song
            {
                Id = new Song.SongId(3), Hash = SongHash.TryCreate(_hash.ToString().ToUpperInvariant()).Value,
                UploadedAt = _createdAt, Info = new SongInfo("Duplicate", "Duplicate", "", "Artist", "Mapper")
            },
            CreateDifficulty(5, 3, 1, EDifficulty.Expert));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(), database.Context, CancellationToken.None);

        result.HasValue.Should().BeFalse();
        (await database.Context.SongDifficulties.AnyAsync(x => x.SSLeaderboardId != null)).Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentImports_ShouldResolveTheSameDifficulty()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var results = await Task.WhenAll(
            PlayerScoresPipeline.ResolveSongDifficultyIdAsync(CreateLeaderboard(), firstContext,
                CancellationToken.None),
            PlayerScoresPipeline.ResolveSongDifficultyIdAsync(CreateLeaderboard(), secondContext,
                CancellationToken.None));

        results.Should().OnlyContain(x => x.HasValue && x.Value == new SongDifficultyId(1));
        (await database.Context.SongDifficulties.CountAsync(x => x.SSLeaderboardId == _leaderboardId)).Should().Be(1);
    }

    [Test]
    public async Task Backfill_ShouldNotSaveUnrelatedTrackedChanges()
    {
        await using var database = await TestDatabase.CreateAsync();
        var gameMode = await database.Context.GameModes.AsTracking().SingleAsync(x => x.Name == "Standard");
        gameMode.Name = "Pending change";

        var result = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(), database.Context, CancellationToken.None);

        result.Value.Should().Be(new SongDifficultyId(1));
        database.Context.ChangeTracker.HasChanges().Should().BeTrue();
        await using var verificationContext = database.CreateContext();
        (await verificationContext.GameModes.SingleAsync(x => x.Id == gameMode.Id)).Name.Should().Be("Standard");
    }

    [Test]
    public async Task ZeroScoreSelectedDifficulty_ShouldFailWithoutStagingChanges()
    {
        await using var database = await TestDatabase.CreateAsync(seedDifficulties: false);
        using var client = CreateClient(HttpStatusCode.NotFound);
        var service = new SongService(database.Context, new BeatLeaderApi(client), new ScoreSaberApi(client));
        var beatMap = CreateBeatMap();
        beatMap.Versions[0].Diffs[0].MaxScore = 0;

        var result = await service.GetOrCreateDifficultyAsync(beatMap, BeatSaverApi.SelectLatestBeatMapVersion,
            await database.Context.GameModes.SingleAsync(x => x.Name == "Standard"), EDifficulty.Expert,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("MaxScore");
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Test]
    public async Task CreateWithoutScoreSaberLeaderboard_ShouldAllowBackfillFromFirstScore()
    {
        await using var database = await TestDatabase.CreateAsync(seedDifficulties: false);
        using var beatLeaderClient =
            CreateClient(HttpStatusCode.OK, JsonSerializer.Serialize(CreateBeatLeaderResponse()));
        using var scoreSaberClient = CreateClient(HttpStatusCode.NotFound);
        var service = new SongService(database.Context, new BeatLeaderApi(beatLeaderClient),
            new ScoreSaberApi(scoreSaberClient));
        var gameMode = await database.Context.GameModes.SingleAsync(x => x.Name == "Standard");

        var result = await service.GetOrCreateDifficultyAsync(
            CreateBeatMap(), BeatSaverApi.SelectLatestBeatMapVersion, gameMode, EDifficulty.Expert,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.difficulty.BLLeaderboardId.Should().Be(BLLeaderboardId.TryCreate("test-leaderboard").Value);
        result.Value.difficulty.SSLeaderboardId.Should().BeNull();
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var resolved = await PlayerScoresPipeline.ResolveSongDifficultyIdAsync(
            CreateLeaderboard(), database.Context, CancellationToken.None);

        resolved.Value.Should().Be(result.Value.difficulty.Id);
        (await database.Context.SongDifficulties.SingleAsync()).SSLeaderboardId.Should().Be(_leaderboardId);
    }

    [Test]
    public async Task TrackedSongWithoutLoadedDifficulties_ShouldReuseExistingDifficulty()
    {
        await using var database = await TestDatabase.CreateAsync();
        var beatLeaderId = BLLeaderboardId.TryCreate("existing-leaderboard").Value;
        await database.Context.SongDifficulties.Where(x => x.Id == new SongDifficultyId(1))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.BLLeaderboardId, beatLeaderId));
        var song = await database.Context.Songs.AsTracking().SingleAsync(x => x.Hash == _hash);
        database.Context.Entry(song).Collection(x => x.SongDifficulties).IsLoaded.Should().BeFalse();
        using var beatLeaderClient = CreateClient(HttpStatusCode.NotFound);
        using var scoreSaberClient = CreateClient(HttpStatusCode.NotFound);
        var service = new SongService(database.Context, new BeatLeaderApi(beatLeaderClient),
            new ScoreSaberApi(scoreSaberClient));
        var gameMode = await database.Context.GameModes.SingleAsync(x => x.Name == "Standard");

        var result = await service.GetOrCreateDifficultyAsync(
            CreateBeatMap(), BeatSaverApi.SelectLatestBeatMapVersion, gameMode, EDifficulty.Expert,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.song.Should().BeSameAs(song);
        result.Value.difficulty.Id.Should().Be(new SongDifficultyId(1));
        result.Value.difficulty.BLLeaderboardId.Should().Be(beatLeaderId);
        result.Value.difficulty.SSLeaderboardId.Should().BeNull();
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Test]
    [Arguments(HttpStatusCode.NotFound, HttpStatusCode.NotFound, "No BeatLeader leaderboard")]
    [Arguments(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable, "Failed to retrieve leaderboard info")]
    public async Task MissingBeatLeaderOrScoreSaberFailure_ShouldNotStageANewDifficulty(
        HttpStatusCode beatLeaderStatus, HttpStatusCode scoreSaberStatus, string expectedError)
    {
        await using var database = await TestDatabase.CreateAsync(seedDifficulties: false);
        using var beatLeaderClient =
            CreateClient(beatLeaderStatus, JsonSerializer.Serialize(CreateBeatLeaderResponse()));
        using var scoreSaberClient = CreateClient(scoreSaberStatus);
        var service = new SongService(database.Context, new BeatLeaderApi(beatLeaderClient),
            new ScoreSaberApi(scoreSaberClient));
        var gameMode = await database.Context.GameModes.SingleAsync(x => x.Name == "Standard");

        var result = await service.GetOrCreateDifficultyAsync(
            CreateBeatMap(), BeatSaverApi.SelectLatestBeatMapVersion, gameMode, EDifficulty.Expert,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedError);
        database.Context.ChangeTracker.HasChanges().Should().BeFalse();
        (await database.Context.Songs.AnyAsync()).Should().BeFalse();
        (await database.Context.SongDifficulties.AnyAsync()).Should().BeFalse();
    }

    private static Leaderboard CreateLeaderboard(
        string? hash = null, string gameMode = "SoloStandard", EDifficulty difficulty = EDifficulty.Expert)
        => new(_createdAt, 1,
            new Difficulty(_leaderboardId, difficulty, SSGameMode.TryCreate(gameMode).Value,
                $"_{difficulty}_{gameMode}"),
            _leaderboardId, new Map(1, hash ?? _hash, null, "Test", "", "Artist", "Mapper", 120, "", false),
            115, new Realm(0, "Default", LeaderboardStatus.Unranked, false, 0, null, null, null), 1);

    private static SongDifficulty CreateDifficulty(long id, int songId, int gameModeId, EDifficulty difficulty) => new()
    {
        Id = new SongDifficultyId(id), SongId = new Song.SongId(songId),
        GameModeId = new GameMode.GameModeId(gameModeId), Difficulty = difficulty,
        Stats = new SongDifficultyStats(MaxScore.CreateUnsafe(115).Value, NJS.CreateUnsafe(10).Value, 1, 0, 0, 1, 1)
    };

    private static BeatMap CreateBeatMap() => new()
    {
        Id = BeatSaverKey.TryCreate("abc12").Value, Name = "Test map", Description = "Test description",
        Uploader = new Uploader { Id = 1, Name = "Mapper", Avatar = "" },
        Metadata = new Metadata
        {
            Bpm = 120, Duration = 60, SongName = "Test song", SongSubName = "",
            LevelAuthorName = "Mapper", SongAuthorName = "Artist"
        },
        Stats = new Stats { Plays = 0, Downloads = 0, Upvotes = 0, Downvotes = 0, Score = 0 },
        Uploaded = "2026-10-11T12:00:00Z", Automapper = false, Ranked = false, Qualified = false,
        Versions =
        [
            new Version
            {
                Hash = _hash, State = "Published", CreatedAt = _createdAt, DownloadURL = "", CoverURL = "",
                PreviewURL = "",
                Diffs =
                [
                    new Diff
                    {
                        Characteristic = "Standard", Difficulty = "Expert", Njs = NJS.TryCreate(10).Value, Offset = 0,
                        Notes = 1,
                        Bombs = 0, Obstacles = 0, Nps = 1, Length = 1, Events = 0, Chroma = false, Me = false,
                        Ne = false, Cinema = false, Seconds = 1, MaxScore = MaxScore.TryCreate(115).Value,
                        ParitySummary = new ParitySummary { Errors = 0, Warns = 0, Resets = 0 }
                    }
                ]
            }
        ]
    };

    private static BLResponses.LeaderboardsResponse CreateBeatLeaderResponse() => new()
    {
        Song = new BLResponses.Song
        {
            Id = "test", Hash = _hash, Name = "Test", Author = "Artist", Mapper = "Mapper", MapperId = 1,
            CoverImage = "", DownloadUrl = "", Bpm = 120, Duration = 60, UploadTime = 0, Difficulties = []
        },
        Leaderboards =
        [
            new BLResponses.LeaderboardsInfoResponse
            {
                Id = BLLeaderboardId.TryCreate("test-leaderboard").Value,
                Difficulty = new BLResponses.DifficultyResponse
                {
                    Id = 1, Value = (int)EDifficulty.Expert, Mode = 1, DifficultyName = "Expert", ModeName = "Standard",
                    Status = default, ModifierValues = null, ModifiersRating = null, NominatedTime = 0,
                    QualifiedTime = 0,
                    RankedTime = 0, SpeedTags = 0, StyleTags = 0, FeatureTags = 0, Stars = null, PredictedAcc = null,
                    PassRating = null, AccRating = null, TechRating = null, Type = 0, Njs = 10, Nps = 1, Notes = 1,
                    Bombs = 0, Walls = 0, MaxScore = 115, Duration = 1, Requirements = default
                }
            }
        ]
    };

    private static HttpClient CreateClient(HttpStatusCode statusCode, string body = "")
        => new(new ResponseHandler(statusCode, body)) { BaseAddress = new Uri("https://leaderboard.test/") };
}