using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.RankedMaps;
using GuildSaber.Api.Features.Scores;
using GuildSaber.Api.Queuing;
using GuildSaber.Api.Shared;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.Guilds.Members;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Color = System.Drawing.Color;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;
using ExMachinaResponse = GuildSaber.Common.Services.BeatLeader.Models.Responses.ExMachinaResponse;
using LackMapCalculation = GuildSaber.Common.Services.BeatLeader.Models.Responses.LackMapCalculation;
using RatingResult = GuildSaber.Common.Services.BeatLeader.Models.Responses.RatingResult;
using Response = GuildSaber.Api.Features.MapRequests.Services.MapProposalService.TransitionResponse;
using ReviewResponse = GuildSaber.Api.Features.MapRequests.Services.MapProposalReviewService.ReviewResponse;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public partial class MapProposalServiceTests : IDisposable
{
    private static readonly DateTimeOffset _createdAt = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _transitionAt = _createdAt.AddHours(1);

    private static readonly CustomCurve _accuracyCurve = new([(1, 1), (0, 0)]);

    // Tests supplying both ratings must never contact BeatLeader.
    private readonly HttpClient _unusedHttpClient = new(new RatingHandler(_
        => throw new InvalidOperationException("Unexpected automatic rating request.")));

    private sealed class UnusedScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
            => throw new InvalidOperationException("Refreshes must only be queued here.");
    }

    private sealed class RecordingQueue : IBackgroundTaskQueue
    {
        public int Count { get; private set; }

        public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
        {
            Count++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class RatingHandler(Func<CancellationToken, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri;
            return Task.FromResult(respond(cancellationToken));
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => _transitionAt;
    }

    // Each test owns a new database. The supplied server must allow creating test databases.
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

        public static async Task<TestDatabase> CreateAsync(MapProposalRating? rating = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("GUILDSABER_TEST_POSTGRES");
            Skip.Unless(!string.IsNullOrWhiteSpace(connectionString),
                "Set GUILDSABER_TEST_POSTGRES to an isolated PostgreSQL server to run persistence tests.");
            var connection = new NpgsqlConnectionStringBuilder(connectionString!)
            {
                Database = $"guildsaber_proposal_test_{Guid.NewGuid():N}", Pooling = false
            };
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            optionsBuilder.WithExpressionExpanding();
            var database = new TestDatabase(optionsBuilder.Options);
            try
            {
                await database.Context.Database.EnsureCreatedAsync();
                await SeedAsync(database.Context, rating);
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        private static async Task SeedAsync(ServerDbContext db, MapProposalRating? rating)
        {
            db.AddRange(
                new Guild
                {
                    Id = new GuildId(1),
                    Info = GuildInfo.TryCreate("Test Guild", "TEST", "Test guild description", Color.White, _createdAt)
                        .Value,
                    Requirements = default, Status = Guild.EGuildStatus.Unverified, DiscordInfo = default
                },
                new Context { Id = new ContextId(1), GuildId = new GuildId(1), Info = new ContextInfo("Main", "") },
                new Context { Id = new ContextId(2), GuildId = new GuildId(1), Info = new ContextInfo("Other", "") },
                new Player
                {
                    Id = new PlayerId(1), Info = new PlayerInfo("Author", "", "", _createdAt), Platform = default,
                    LinkedAccounts = default, SubscriptionInfo = default
                },
                new GameMode { Id = new GameMode.GameModeId(1), Name = "Standard" },
                new PlayMode { Id = new PlayMode.PlayModeId(1), Name = "Standard" },
                new Song
                {
                    Id = new Song.SongId(1), Hash = SongHash.CreateUnsafe(new string('a', 40)).Value,
                    UploadedAt = _createdAt,
                    Info = new SongInfo("Test", "Test", "", "Author", "Mapper")
                },
                new SongDifficulty
                {
                    Id = new SongDifficultyId(1), SongId = new Song.SongId(1), GameModeId = new GameMode.GameModeId(1),
                    Difficulty = EDifficulty.ExpertPlus,
                    Stats = new SongDifficultyStats(MaxScore.CreateUnsafe(115).Value, NJS.CreateUnsafe(10).Value,
                        1, 0, 0, 1, 1)
                },
                new Category
                {
                    Id = new CategoryId(1), GuildId = new GuildId(1),
                    Info = CategoryInfo.TryCreate("Category", "Test category").Value
                },
                new RankedMapListAchievement(new Achievement.AchievementId(1), new GuildId(1), new ContextId(1), null,
                    new AchievementInfo("Achievement", Color.White), default, null, false, default, 1),
                new MapRequestThread
                {
                    Id = new MapRequestThreadId(1), ContextId = new ContextId(1), AuthorId = new PlayerId(1),
                    CreatedAt = _createdAt
                },
                new DraftProposal
                {
                    Id = new MapProposalId(1), ThreadId = new MapRequestThreadId(1), AuthorId = new PlayerId(1),
                    CreatedAt = _createdAt,
                    Rating = rating ?? new MapProposalRating
                    {
                        AccStar = new RankedMapRating.AccuracyStar(2), DiffStar = new RankedMapRating.DifficultyStar(5)
                    },
                    Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
                    MapVersions =
                    [
                        new MapProposalVersion
                        {
                            SongDifficultyId = new SongDifficultyId(1), PlayModeId = new PlayMode.PlayModeId(1),
                            PlayModeName = "Standard"
                        }
                    ],
                    Categories = [new MapProposalCategory { CategoryId = new CategoryId(1), Name = "Category" }],
                    Achievements =
                    [
                        new MapProposalAchievement
                            { AchievementId = new Achievement.AchievementId(1), Name = "Achievement" }
                    ]
                });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }

    public void Dispose() => _unusedHttpClient.Dispose();

    [Test]
    public async Task SubmitThenClose_ShouldPreserveIdentitySnapshotsAndVotes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var service = CreateService(db);

        var result = await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);
        var submitted = result.Should().BeOfType<Response.Success>().Which.Proposal
            .Should().BeOfType<SubmittedProposal>().Which;
        submitted.Id.Should().Be(new MapProposalId(1));
        submitted.ThreadId.Should().Be(new MapRequestThreadId(1));
        submitted.CreatedAt.Should().Be(_createdAt);
        submitted.SubmittedAt.Should().Be(_transitionAt);
        submitted.Rating.DiffStar.Should().Be(new RankedMapRating.DifficultyStar(5));
        submitted.Rating.IsAccStarAutoCalculated.Should().BeFalse();
        submitted.Rating.IsDiffStarAutoCalculated.Should().BeFalse();
        submitted.MapVersions.Should().ContainSingle();
        submitted.Categories.Should().ContainSingle();
        submitted.Achievements.Should().ContainSingle();

        db.AddRange(
            new CommunityProposalVote
            {
                ProposalId = submitted.Id, PlayerId = new PlayerId(1), IsPositive = true, VotedAt = _transitionAt
            },
            new RankingTeamProposalVote
            {
                ProposalId = submitted.Id, PlayerId = new PlayerId(1), IsPositive = false,
                VotedAt = _transitionAt, Reason = "The difficulty rating needs adjustment."
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Historical references can disappear. This must not stop the author withdrawing the proposal.
        await db.Categories.ExecuteDeleteAsync();
        const string reason = "I'd like to revise the difficulty rating.";
        var closedResult = await service.CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), reason,
            CancellationToken.None);
        var closed = closedResult.Should().BeOfType<Response.Success>().Which.Proposal
            .Should().BeOfType<ClosedProposal>().Which;
        closed.Id.Should().Be(submitted.Id);
        closed.ThreadId.Should().Be(submitted.ThreadId);
        closed.SubmittedAt.Should().Be(submitted.SubmittedAt);
        closed.ClosedAt.Should().Be(_transitionAt);
        closed.ClosedById.Should().Be(new PlayerId(1));
        closed.Reason.Should().Be(reason);
        closed.Rating.DiffStar.Should().Be(submitted.Rating.DiffStar);
        closed.Categories.Single().Name.Should().Be("Category");
        (await db.MapProposalVotes.CountAsync()).Should().Be(2);
        (await db.MapProposalVotes.OfType<RankingTeamProposalVote>().SingleAsync()).Reason
            .Should().Be("The difficulty rating needs adjustment.");
        (await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.InvalidState>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task WrongAuthorOrContext_ShouldNotTransition(bool wrongContext)
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var result = await service.SubmitAsync(new ContextId(wrongContext ? 2 : 1), new MapProposalId(1),
            new PlayerId(wrongContext ? 1 : 2), CancellationToken.None);

        if (wrongContext) result.Should().BeOfType<Response.NotFound>();
        else result.Should().BeOfType<Response.Forbidden>();
        (await database.Context.MapProposals.SingleAsync()).Should().BeOfType<DraftProposal>();
    }

    [Test]
    [Arguments(nameof(MapProposal.Categories))]
    [Arguments(nameof(MapProposal.Achievements))]
    [Arguments(nameof(MapProposal.MapVersions))]
    public async Task DeletedReference_ShouldPreventSubmission(string field)
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        if (field == nameof(MapProposal.Categories)) await db.Categories.ExecuteDeleteAsync();
        else if (field == nameof(MapProposal.Achievements)) await db.Achievements.ExecuteDeleteAsync();
        else await db.PlayModes.ExecuteDeleteAsync();

        var service = CreateService(db);
        var result = await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);

        result.Should().BeOfType<Response.ValidationFailure>().Which.Errors.Should().Contain(x => x.Key == field);
        (await db.MapProposals.SingleAsync()).Should().BeOfType<DraftProposal>();
    }

    [Test]
    public async Task AchievementFromAnotherContext_ShouldPreventSubmission()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        db.Achievements.Add(new RankedMapListAchievement(new Achievement.AchievementId(2), new GuildId(1),
            new ContextId(2), null,
            new AchievementInfo("Other context", Color.White), default, null, false, default, 1));
        db.Add(new MapProposalAchievement
        {
            ProposalId = new MapProposalId(1), AchievementId = new Achievement.AchievementId(2), Name = "Other context"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await CreateService(db)
            .SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);

        result.Should().BeOfType<Response.ValidationFailure>().Which.Errors
            .Should().Contain(x => x.Key == nameof(MapProposal.Achievements));
    }

    [Test]
    public async Task ConcurrentSubmissions_ShouldOnlyApplyOnce()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = CreateService(firstContext);
        var second = CreateService(secondContext);

        var results = await Task.WhenAll(
            first.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None),
            second.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None));

        results.Count(x => x is Response.Success).Should().Be(1);
        results.Count(x => x is Response.Conflict or Response.InvalidState).Should().Be(1);
        (await database.Context.MapProposals.OfType<SubmittedProposal>().CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task Transition_ShouldDetachOldSubtypeWithoutDiscardingUnrelatedTrackedChanges()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var draft = await db.MapProposals.AsTracking().Include(x => x.MapVersions).SingleAsync();
        var player = await db.Players.AsTracking().SingleAsync();
        player.IsManager = true;

        var result = await CreateService(db)
            .SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);

        result.Should().BeOfType<Response.Success>();
        db.Entry(draft).State.Should().Be(EntityState.Detached);
        db.ChangeTracker.Entries<MapProposalVersion>().Should().BeEmpty();
        db.Entry(player).State.Should().Be(EntityState.Modified);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task UnsavedConfiguration_ShouldNotBeSilentlyDiscarded(bool snapshotChanged)
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var draft = await db.MapProposals.AsTracking().Include(x => x.MapVersions).SingleAsync();
        if (snapshotChanged)
            db.Entry(draft.MapVersions.Single()).Property(x => x.Order).CurrentValue = 1;
        else
            draft.Rating.DiffStar = new RankedMapRating.DifficultyStar(6);

        var service = CreateService(db);
        var submit = ()
            => service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);

        await submit.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Save or discard pending proposal changes before transitioning it.");
        db.ChangeTracker.HasChanges().Should().BeTrue();
        var persisted = await db.MapProposals.SingleAsync();
        persisted.Should().BeOfType<DraftProposal>();
        persisted.Rating.DiffStar.Should().Be(new RankedMapRating.DifficultyStar(5));
    }

    [Test]
    [Arguments(true, true, AbstractScore.EModifiers.None)]
    [Arguments(false, true, AbstractScore.EModifiers.FasterSong)]
    [Arguments(true, false, AbstractScore.EModifiers.SlowerSong)]
    [Arguments(true, true, AbstractScore.EModifiers.SuperFastSong)]
    public async Task Submit_ShouldResolveMissingRatingsAndPreserveTheirOrigin(
        bool automaticAccuracy, bool automaticDifficulty, AbstractScore.EModifiers modifiers)
    {
        var rating = new MapProposalRating
        {
            AccStar = automaticAccuracy ? null : new RankedMapRating.AccuracyStar(2),
            DiffStar = automaticDifficulty ? null : new RankedMapRating.DifficultyStar(5)
        };
        await using var database = await TestDatabase.CreateAsync(rating);
        var db = database.Context;
        await db.MapProposals.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Requirements.MandatoryModifiers, modifiers));
        if (automaticAccuracy)
            await AddAccuracyCurveAsync(db);

        var exMachina = CreateExMachinaResponse();
        using var handler = new RatingHandler(_ => JsonResponse(JsonSerializer.Serialize(exMachina)));
        using var client = new HttpClient(handler);
        var service = CreateService(db, new BeatLeaderApi(client));

        var result = await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);

        var submitted = result.Should().BeOfType<Response.Success>().Which.Proposal
            .Should().BeOfType<SubmittedProposal>().Which;
        var calculated = ScoringUtils.GetExMachinaRating(exMachina, submitted.Requirements);
        submitted.Rating.AccStar.Should().Be(rating.AccStar ?? calculated.ToAccStar(_accuracyCurve));
        submitted.Rating.DiffStar.Should().Be(rating.DiffStar ?? calculated.ToDiffStar());
        submitted.Rating.IsAccStarAutoCalculated.Should().Be(automaticAccuracy);
        submitted.Rating.IsDiffStarAutoCalculated.Should().Be(automaticDifficulty);
        handler.RequestCount.Should().Be(1);
        handler.RequestUri!.AbsolutePath.Should()
            .Be($"/ppai2/{new string('a', 40)}/Standard/{(int)EDifficulty.ExpertPlus}");

        // Closing preserves the frozen ratings and provenance, without recalculating them.
        var closed = await service.CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), null,
            CancellationToken.None);
        closed.Should().BeOfType<Response.Success>().Which.Proposal.Rating.Should().Be(submitted.Rating);
        handler.RequestCount.Should().Be(1);
        (await db.MapProposals.SingleAsync()).Rating.Should().Be(submitted.Rating);
    }

    [Test]
    public async Task AutomaticRating_ShouldUseTheLowestOrderMapVersion()
    {
        await using var database = await TestDatabase.CreateAsync(new MapProposalRating
        {
            AccStar = new RankedMapRating.AccuracyStar(2)
        });
        var db = database.Context;
        var original = await db.SongDifficulties.SingleAsync();
        db.SongDifficulties.Add(new SongDifficulty
        {
            Id = new SongDifficultyId(2), SongId = original.SongId, GameModeId = original.GameModeId,
            Difficulty = EDifficulty.Expert, Stats = original.Stats
        });
        await db.MapProposalVersions.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Order, (byte)1));
        db.MapProposalVersions.Add(new MapProposalVersion
        {
            ProposalId = new MapProposalId(1), SongDifficultyId = new SongDifficultyId(2),
            PlayModeId = new PlayMode.PlayModeId(1), PlayModeName = "Standard", Order = 0
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var handler = new RatingHandler(_ => JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse())));
        using var client = new HttpClient(handler);
        var service = CreateService(db, new BeatLeaderApi(client));

        var result = await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);

        result.Should().BeOfType<Response.Success>();
        handler.RequestUri!.AbsolutePath.Should()
            .Be($"/ppai2/{new string('a', 40)}/Standard/{(int)EDifficulty.Expert}");
    }

    [Test]
    [Arguments("not-found")]
    [Arguments("server-error")]
    [Arguments("null")]
    [Arguments("invalid-json")]
    [Arguments("missing-calculation")]
    [Arguments("incomplete-calculation")]
    [Arguments("non-finite-calculation")]
    [Arguments("network-error")]
    [Arguments("timeout")]
    [Arguments("missing-modifier")]
    [Arguments("invalid-rating")]
    public async Task CalculationFailure_ShouldLeaveDraftAndRatingsUnchanged(string failure)
    {
        var rating = new MapProposalRating { AccStar = new RankedMapRating.AccuracyStar(2) };
        await using var database = await TestDatabase.CreateAsync(rating);
        using var handler = new RatingHandler(_ => failure switch
        {
            "not-found" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "server-error" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            "null" => JsonResponse("null"),
            "invalid-json" => JsonResponse("{"),
            "missing-calculation" => JsonResponse("""{"FS":{},"SFS":{},"BFS":{},"BSF":{},"SS":{},"None":{}}"""),
            "incomplete-calculation" => JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse())
                .Replace("\"balanced_pass_diff\":4,", "")),
            "non-finite-calculation" => JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse())
                .Replace("\"balanced_pass_diff\":4", "\"balanced_pass_diff\":1e100")),
            "network-error" => throw new HttpRequestException("Network unavailable."),
            "timeout" => throw new TaskCanceledException("Rating request timed out."),
            "missing-modifier" => JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse(nullModifier: true))),
            "invalid-rating" => JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse(negativeRating: true))),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        });
        using var client = new HttpClient(handler);
        var service = CreateService(database.Context, new BeatLeaderApi(client));

        var submit = () => service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);

        if (failure == "network-error")
            await submit.Should().ThrowAsync<HttpRequestException>();
        else if (failure == "timeout")
            await submit.Should().ThrowAsync<OperationCanceledException>();
        else
            (await submit()).Should().BeOfType<Response.RatingCalculationFailure>()
                .Which.Message.Should().NotBeNullOrWhiteSpace();

        var persisted = await database.Context.MapProposals.SingleAsync();
        persisted.Should().BeOfType<DraftProposal>();
        persisted.Rating.Should().Be(rating);
    }

    [Test]
    public async Task MissingAccuracyCurve_ShouldLeaveDraftUnchangedWithoutCallingBeatLeader()
    {
        var rating = new MapProposalRating { DiffStar = new RankedMapRating.DifficultyStar(5) };
        await using var database = await TestDatabase.CreateAsync(rating);
        var service = CreateService(database.Context);

        var result = await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);

        result.Should().BeOfType<Response.RatingCalculationFailure>().Which.Message.Should().Contain("accuracy curve");
        var persisted = await database.Context.MapProposals.SingleAsync();
        persisted.Should().BeOfType<DraftProposal>();
        persisted.Rating.Should().Be(rating);
    }

    [Test]
    public async Task CancelledCalculation_ShouldPropagateCancellationAndLeaveDraftUnchanged()
    {
        var rating = new MapProposalRating { AccStar = new RankedMapRating.AccuracyStar(2) };
        await using var database = await TestDatabase.CreateAsync(rating);
        using var cancellation = new CancellationTokenSource();
        using var handler = new RatingHandler(token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Expected cancellation.");
        });
        using var client = new HttpClient(handler);
        var service = CreateService(database.Context, new BeatLeaderApi(client));
        var token = cancellation.Token;
        var submit = () => service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), token);

        await submit.Should().ThrowAsync<OperationCanceledException>();
        var persisted = await database.Context.MapProposals.SingleAsync();
        persisted.Should().BeOfType<DraftProposal>();
        persisted.Rating.Should().Be(rating);
    }

    [Test]
    public async Task Database_ShouldRejectSubmissionWithUnresolvedRatings()
    {
        await using var database = await TestDatabase.CreateAsync(new MapProposalRating());
        var db = database.Context;
        var update = () => db.MapProposals.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => EF.Property<MapProposalStatus>(x, "status"), MapProposalStatus.Submitted)
            .SetProperty(x => ((SubmittedProposal)x).SubmittedAt, _transitionAt));

        var exception = await update.Should().ThrowAsync<PostgresException>();
        exception.Which.ConstraintName.Should().Be("CK_MapProposals_ResolvedRating");
    }

    [Test]
    public async Task AcceptThenAcceptRevision_ShouldKeepMapIdentityAndHistoryButNotCloseDrafts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        await db.Players.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsManager, true));
        db.SongDifficulties.Add(new SongDifficulty
        {
            Id = new SongDifficultyId(2), SongId = new Song.SongId(1), GameModeId = new GameMode.GameModeId(1),
            Difficulty = EDifficulty.Expert,
            Stats = (await db.SongDifficulties.SingleAsync()).Stats
        });
        db.Add(new MapProposalVersion
        {
            ProposalId = new MapProposalId(1), SongDifficultyId = new SongDifficultyId(2),
            PlayModeId = new PlayMode.PlayModeId(1), PlayModeName = "Standard", Order = 1
        });
        var revision = CreateDraft(3);
        revision.Rating.DiffStar = new RankedMapRating.DifficultyStar(9);
        revision.Categories.Clear();
        revision.Achievements.Clear();
        db.MapProposals.AddRange(CreateDraft(2), revision);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var transitions = CreateService(db);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None);
        db.MapRequestThreadBoosts.Add(new MapRequestThreadBoost
            { ThreadId = new MapRequestThreadId(1), PlayerId = new PlayerId(1) });
        await db.SaveChangesAsync();
        var queue = new RecordingQueue();
        var reviews = CreateReviewService(db, queue);

        var first = (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                CancellationToken.None,
                bypassVoteRequirements: true))
            .Should().BeOfType<ReviewResponse.Accepted>().Which;

        first.AddedDifficultyIds.Should().BeEquivalentTo([new SongDifficultyId(1), new SongDifficultyId(2)]);
        (await db.MapProposals.SingleAsync(x => x.Id == new MapProposalId(2))).Should().BeOfType<ClosedProposal>();
        (await db.MapProposals.SingleAsync(x => x.Id == new MapProposalId(3))).Should().BeOfType<DraftProposal>();
        (await db.MapRequestThreadBoosts.CountAsync()).Should().Be(0);
        var original = await db.RankedMaps.Include(x => x.Categories).Include(x => x.Achievements)
            .Include(x => x.MapVersions).SingleAsync();
        original.Categories.Should().ContainSingle();
        original.Achievements.Should().ContainSingle();
        original.MapVersions.Should().HaveCount(2);

        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(3), new PlayerId(1), CancellationToken.None);
        var second =
            (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(3), new PlayerId(1), CancellationToken.None,
                bypassVoteRequirements: true))
            .Should().BeOfType<ReviewResponse.Accepted>().Which;

        second.RankedMapId.Should().Be(first.RankedMapId);
        second.AddedDifficultyIds.Should().BeEmpty();
        second.Proposal.AcceptedAt.Should().BeAfter(first.Proposal.AcceptedAt);
        var edited = await db.RankedMaps.Include(x => x.Categories).Include(x => x.Achievements)
            .Include(x => x.MapVersions).SingleAsync();
        edited.Info.CreatedAt.Should().Be(original.Info.CreatedAt);
        edited.MapVersions.Should().ContainSingle().Which.SongDifficultyId.Should().Be(new SongDifficultyId(1));
        edited.MapVersions.Single().AddedAt.Should()
            .Be(original.MapVersions.Single(x => x.SongDifficultyId == new SongDifficultyId(1)).AddedAt);
        edited.Rating.DiffStar.Should().Be(new RankedMapRating.DifficultyStar(9));
        edited.Categories.Should().BeEmpty();
        edited.Achievements.Should().BeEmpty();
        var historical = await db.MapProposals.OfType<AcceptedProposal>().Include(x => x.Categories)
            .Include(x => x.MapVersions)
            .SingleAsync(x => x.Id == new MapProposalId(1));
        historical.Rating.DiffStar.Should().Be(new RankedMapRating.DifficultyStar(5));
        historical.Categories.Single().Name.Should().Be("Category");
        historical.MapVersions.Should().HaveCount(2);
        queue.Count.Should().Be(2);
    }

    [Test]
    public async Task Acceptance_ShouldOnlyCountRankingTeamVotes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        await CreateService(db)
            .SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        var queue = new RecordingQueue();
        var reviews = CreateReviewService(db, queue);
        db.MapProposalVotes.Add(new CommunityProposalVote
        {
            ProposalId = new MapProposalId(1), PlayerId = new PlayerId(1), IsPositive = true, VotedAt = _transitionAt
        });
        await db.SaveChangesAsync();
        (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().Be(new ReviewResponse.VoteRequirementsNotMet(0, 0, 3, true));

        for (var id = 1; id <= 4; id++)
        {
            if (id > 1)
                db.Players.Add(new Player
                {
                    Id = new PlayerId(id), Info = new PlayerInfo($"Reviewer {id}", "", "", _createdAt),
                    Platform = default, LinkedAccounts = default, SubscriptionInfo = default
                });
            db.MapProposalVotes.Add(new RankingTeamProposalVote
            {
                ProposalId = new MapProposalId(1), PlayerId = new PlayerId(id), IsPositive = id < 4,
                VotedAt = _transitionAt
            });
        }

        await db.SaveChangesAsync();
        (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().Be(new ReviewResponse.VoteRequirementsNotMet(3, 1, 3, true));
        (await db.RankedMaps.CountAsync()).Should().Be(0);
        queue.Count.Should().Be(0);

        await db.MapProposalVotes.OfType<RankingTeamProposalVote>().Where(x => !x.IsPositive).ExecuteDeleteAsync();
        (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<ReviewResponse.Accepted>();
        (await db.MapProposalVotes.CountAsync()).Should().Be(4);
        queue.Count.Should().Be(1);
    }

    [Test]
    public async Task MissingReference_ShouldBlockAcceptanceButAllowDenialAndReleaseOnlyLastBoosts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        db.Members.Add(new Member
        {
            GuildId = new GuildId(1), PlayerId = new PlayerId(1), CreatedAt = _createdAt, EditedAt = _createdAt,
            Permissions = Member.EPermission.GuildLeader, JoinState = Member.EJoinState.Joined, Priority = 0
        });
        db.MapProposals.AddRange(CreateDraft(2), CreateDraft(3));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var transitions = CreateService(db);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None);
        db.MapRequestThreadBoosts.Add(new MapRequestThreadBoost
            { ThreadId = new MapRequestThreadId(1), PlayerId = new PlayerId(1) });
        await db.SaveChangesAsync();
        await db.Categories.ExecuteDeleteAsync();
        var queue = new RecordingQueue();
        var reviews = CreateReviewService(db, queue);

        (await reviews.AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None,
                bypassVoteRequirements: true))
            .Should().BeOfType<ReviewResponse.ValidationFailure>();
        (await db.MapProposals.OfType<SubmittedProposal>().CountAsync()).Should().Be(2);
        (await db.MapRequestThreadBoosts.CountAsync()).Should().Be(1);
        (await db.RankedMaps.CountAsync()).Should().Be(0);

        var denied = (await reviews.DenyAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                "Category removed", CancellationToken.None))
            .Should().BeOfType<ReviewResponse.Denied>().Which.Proposal;
        denied.Reason.Should().Be("Category removed");
        denied.Categories.Single().Name.Should().Be("Category");
        (await db.MapRequestThreadBoosts.CountAsync()).Should().Be(1);
        (await reviews.DenyAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), null, CancellationToken.None))
            .Should().BeOfType<ReviewResponse.Denied>();
        (await db.MapRequestThreadBoosts.CountAsync()).Should().Be(0);
        (await db.MapProposals.SingleAsync(x => x.Id == new MapProposalId(3))).Should().BeOfType<DraftProposal>();
        queue.Count.Should().Be(0);
    }

    [Test]
    public async Task ConcurrentAcceptances_ShouldOnlyApplyOneProposalAndQueueOneRefresh()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        await db.Players.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsManager, true));
        db.MapProposals.Add(CreateDraft(2));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var transitions = CreateService(db);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        await transitions.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None);
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstQueue = new RecordingQueue();
        var secondQueue = new RecordingQueue();

        var results = await Task.WhenAll(
            CreateReviewService(firstContext, firstQueue).AcceptAsync(new ContextId(1), new MapProposalId(1),
                new PlayerId(1), CancellationToken.None,
                bypassVoteRequirements: true),
            CreateReviewService(secondContext, secondQueue)
                .AcceptAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None,
                    bypassVoteRequirements: true));

        results.Count(x => x is ReviewResponse.Accepted).Should().Be(1);
        results.Count(x => x is ReviewResponse.Conflict or ReviewResponse.InvalidState).Should().Be(1);
        (await db.RankedMaps.CountAsync()).Should().Be(1);
        (await db.MapProposals.OfType<AcceptedProposal>().CountAsync()).Should().Be(1);
        (await db.MapProposals.OfType<ClosedProposal>().CountAsync()).Should().Be(1);
        (firstQueue.Count + secondQueue.Count).Should().Be(1);
    }

    private static DraftProposal CreateDraft(int id) => new()
    {
        Id = new MapProposalId(id), ThreadId = new MapRequestThreadId(1), AuthorId = new PlayerId(1),
        CreatedAt = _createdAt,
        Rating = new MapProposalRating
            { AccStar = new RankedMapRating.AccuracyStar(2), DiffStar = new RankedMapRating.DifficultyStar(5) },
        Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
        MapVersions =
        [
            new MapProposalVersion
            {
                SongDifficultyId = new SongDifficultyId(1), PlayModeId = new PlayMode.PlayModeId(1),
                PlayModeName = "Standard"
            }
        ],
        Categories = [new MapProposalCategory { CategoryId = new CategoryId(1), Name = "Category" }],
        Achievements =
            [new MapProposalAchievement { AchievementId = new Achievement.AchievementId(1), Name = "Achievement" }]
    };

    [Test]
    public async Task QueueAndHistory_ShouldHideOtherDraftsAndSerializeAllLifecycleStates()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var queries = new MapRequestQueries(db, new FixedTimeProvider(), Options.Create(new MapRequestSettings()),
            Options.Create(new MapRequestBoostSettings()), Options.Create(new MapProposalReviewSettings()));
        (await queries.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1), null, CancellationToken.None))
            .Should().BeNull();
        (await queries.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                CancellationToken.None))!.Proposals.Should()
            .ContainSingle();
        db.MapProposals.Add(CreateDraft(2));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await CreateService(db)
            .SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        var votes = new MapProposalVoteService(db, new FixedTimeProvider());
        (await votes.SetCommunityAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), true,
                CancellationToken.None))
            .Should().BeOfType<MapProposalVoteService.VoteResponse.Success>();
        foreach (var sort in Enum.GetValues<MapProposalRequests.EMapRequestSorter>())
        foreach (var order in Enum.GetValues<EOrder>())
        {
            var queue = await queries.GetQueueAsync(new ContextId(1), new PlayerId(1),
                new MapProposalRequests.Filters(), sort, order, 1, 10, CancellationToken.None);
            queue.Data.Should().ContainSingle().Which.Proposals.Should().ContainSingle()
                .Which.Should().BeOfType<MapRequestResponses.Proposal.Submitted>();
        }

        foreach (var sort in Enum.GetValues<MapProposalRequests.EMapProposalSorter>())
        foreach (var order in Enum.GetValues<EOrder>())
        {
            var mine = await queries.GetMineAsync(new ContextId(1), new PlayerId(1),
                new MapProposalRequests.Filters(Search: "Test", IsRevision: false),
                sort, order, 1, 10, CancellationToken.None);
            mine.Data.Select(x => x.Id.Value).Should()
                .Equal(order == EOrder.Asc ? [1L, 2L] : [2L, 1L]);
        }

        (await queries.GetQueueAsync(new ContextId(1), null,
            new MapProposalRequests.Filters(Search: new string('a', 40)),
            MapProposalRequests.EMapRequestSorter.Priority, EOrder.Desc, 1, 10,
            CancellationToken.None)).Data.Should().ContainSingle();

        (await queries.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1), null, CancellationToken.None))!
            .Proposals.Should()
            .ContainSingle();
        var ownerView = (await queries.GetThreadAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
            CancellationToken.None))!;
        ownerView.Proposals.Should().HaveCount(2);
        var json = JsonSerializer.Serialize(ownerView);
        JsonSerializer.Deserialize<MapRequestResponses.Thread>(json)!.Proposals
            .Should().Contain(x => x is MapRequestResponses.Proposal.Draft)
            .And.Contain(x => x is MapRequestResponses.Proposal.Submitted);

        await CreateService(db).CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), "Revise",
            CancellationToken.None);
        (await votes.RemoveCommunityAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                CancellationToken.None))
            .Should().BeOfType<MapProposalVoteService.VoteResponse.InvalidState>();
        (await db.MapProposalVotes.CountAsync()).Should().Be(1);
        (await queries.GetQueueAsync(new ContextId(1), null, new MapProposalRequests.Filters(),
            MapProposalRequests.EMapRequestSorter.Priority,
            EOrder.Desc, 1, 10,
            CancellationToken.None)).Data.Should().BeEmpty();
        (await queries.GetAllowanceAsync(new ContextId(1), new PlayerId(1), CancellationToken.None)).Drafts.Should()
            .Be(1);
    }

    [Test]
    public async Task Submission_ShouldReturnDuplicateResourcesBeforeForceAndEnforceItsOwnQuota()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        db.MapRequestThreads.Add(new MapRequestThread
        {
            Id = new MapRequestThreadId(2), ContextId = new ContextId(1), AuthorId = new PlayerId(1),
            CreatedAt = _createdAt
        });
        var second = CreateDraft(2);
        db.MapProposals.Add(second);
        db.Entry(second).Property(x => x.ThreadId).CurrentValue = new MapRequestThreadId(2);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);
        await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None);
        var warning = (await service.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1),
                CancellationToken.None))
            .Should().BeOfType<Response.Duplicates>().Which;
        warning.Matches.Should().ContainSingle().Which.ProposalId.Should().Be(new MapProposalId(1));
        (await db.MapProposals.SingleAsync(x => x.Id == new MapProposalId(2))).Should().BeOfType<DraftProposal>();
        var limited = new MapProposalService(db, new FixedTimeProvider(), new BeatLeaderApi(_unusedHttpClient),
            Options.Create(new MapRequestSettings { MaxRankingRequestsPerPlayer = 1 }));
        (await limited.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None,
                force: true))
            .Should().BeOfType<Response.LimitReached>();
        (await service.SubmitAsync(new ContextId(1), new MapProposalId(2), new PlayerId(1), CancellationToken.None,
                force: true))
            .Should().BeOfType<Response.Success>();
    }

    [Test]
    public async Task ImportedAcceptance_ShouldKeepSystemHistoryAndNeverImpersonateAPlayer()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        await db.MapRequestThreads.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsLegacyImport, true));
        await db.MapProposals.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AuthorId, (PlayerId?)null));
        (await CreateService(db).SubmitImportedAsync(new ContextId(1), new MapProposalId(1), CancellationToken.None))
            .Should()
            .BeOfType<Response.Success>();
        var queue = new RecordingQueue();
        var accepted =
            (await CreateReviewService(db, queue)
                .AcceptImportedAsync(new ContextId(1), new MapProposalId(1), CancellationToken.None))
            .Should().BeOfType<ReviewResponse.Accepted>().Which;
        accepted.Proposal.AuthorId.Should().BeNull();
        accepted.Proposal.AcceptedById.Should().BeNull();
        queue.Count.Should().Be(0);
        (await db.RankedMaps.SingleAsync()).ThreadId.Should().Be(new MapRequestThreadId(1));
    }

    private MapProposalReviewService CreateReviewService(ServerDbContext db, RecordingQueue queue)
    {
        var scopeFactory = new UnusedScopeFactory();
        var rankedMaps = new RankedMapLimitService(db, Options.Create(new RankedMapSettings
        {
            DefaultSettings = new RankedMapDefaultSettings { MaxRankedMapCount = 1 },
            BoostSettings = new RankedMapBoostSettings()
        }));
        return new MapProposalReviewService(db, CreateService(db), rankedMaps, new FixedTimeProvider(),
            Options.Create(new MapProposalReviewSettings()), queue, scopeFactory);
    }

    private static async Task AddAccuracyCurveAsync(ServerDbContext db)
    {
        db.Points.Add(new Point
        {
            GuildId = new GuildId(1), Info = PointInfo.TryCreate("PP", "Test points").Value,
            CurveSettings = new CurveSettings { Accuracy = _accuracyCurve, Difficulty = _accuracyCurve }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static ExMachinaResponse CreateExMachinaResponse(bool nullModifier = false, bool negativeRating = false)
    {
        var normal = new RatingResult
        {
            PredictedAcc = 0.96f,
            LackMapCalculation = new LackMapCalculation { PassRating = negativeRating ? -4 : 4, TechRating = 0.2f }
        };
        var faster = new RatingResult
        {
            PredictedAcc = 0.94f,
            LackMapCalculation = new LackMapCalculation { PassRating = 7, TechRating = 0.3f }
        };
        return new ExMachinaResponse
        {
            None = nullModifier ? null! : normal, SS = normal, FS = faster, SFS = faster, BFS = normal, BSF = normal
        };
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private MapProposalService CreateService(ServerDbContext db, BeatLeaderApi? beatLeaderApi = null)
        => new(db, new FixedTimeProvider(), beatLeaderApi ?? new BeatLeaderApi(_unusedHttpClient),
            Options.Create(new MapRequestSettings()));
}
