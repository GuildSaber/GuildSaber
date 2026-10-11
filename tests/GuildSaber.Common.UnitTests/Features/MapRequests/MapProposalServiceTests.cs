using System.Drawing;
using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Categories;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Snapshots;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties.GameModes;
using GuildSaber.Database.Models.StrongTypes;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;
using Response = GuildSaber.Api.Features.MapRequests.MapProposalService.TransitionResponse;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public class MapProposalServiceTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _transitionAt = _createdAt.AddHours(1);

    [Test]
    public async Task SubmitThenClose_ShouldPreserveIdentitySnapshotsAndVotes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var service = new MapProposalService(db, new FixedTimeProvider());

        var result = await service.SubmitAsync(new(1), new(1), new(1), CancellationToken.None);
        var submitted = result.Should().BeOfType<Response.Success>().Which.Proposal
            .Should().BeOfType<SubmittedProposal>().Which;
        submitted.Id.Should().Be(new MapProposalId(1));
        submitted.ThreadId.Should().Be(new MapRequestThreadId(1));
        submitted.CreatedAt.Should().Be(_createdAt);
        submitted.SubmittedAt.Should().Be(_transitionAt);
        submitted.Rating.DiffStar.Value.Should().Be(5);
        submitted.MapVersions.Should().ContainSingle();
        submitted.Categories.Should().ContainSingle();
        submitted.Achievements.Should().ContainSingle();

        db.AddRange(
            new CommunityProposalVote
            {
                ProposalId = submitted.Id, PlayerId = new(1), IsPositive = true, VotedAt = _transitionAt
            },
            new RankingTeamProposalVote
            {
                ProposalId = submitted.Id, PlayerId = new(1), IsPositive = false,
                VotedAt = _transitionAt, Reason = "The difficulty rating needs adjustment."
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Historical references can disappear; this must not stop the author withdrawing the proposal.
        await db.Categories.ExecuteDeleteAsync();
        const string reason = "I'd like to revise the difficulty rating.";
        var closedResult = await service.CloseAsync(new(1), new(1), new(1), reason, CancellationToken.None);
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
        (await service.SubmitAsync(new(1), new(1), new(1), CancellationToken.None))
            .Should().BeOfType<Response.InvalidState>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task WrongAuthorOrContext_ShouldNotTransition(bool wrongContext)
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new MapProposalService(database.Context, new FixedTimeProvider());

        var result = await service.SubmitAsync(new(wrongContext ? 2 : 1), new(1),
            new(wrongContext ? 1 : 2), CancellationToken.None);

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

        var service = new MapProposalService(db, new FixedTimeProvider());
        var result = await service.SubmitAsync(new(1), new(1), new(1), CancellationToken.None);

        result.Should().BeOfType<Response.ValidationFailure>().Which.Errors.Should().Contain(x => x.Key == field);
        (await db.MapProposals.SingleAsync()).Should().BeOfType<DraftProposal>();
    }

    [Test]
    public async Task AchievementFromAnotherContext_ShouldPreventSubmission()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        db.Achievements.Add(new RankedMapListAchievement(new(2), new(1), new(2), null,
            new AchievementInfo("Other context", Color.White), default, null, false, default, 1));
        db.Add(new MapProposalAchievement { ProposalId = new(1), AchievementId = new(2), Name = "Other context" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await new MapProposalService(db, new FixedTimeProvider())
            .SubmitAsync(new(1), new(1), new(1), CancellationToken.None);

        result.Should().BeOfType<Response.ValidationFailure>().Which.Errors
            .Should().Contain(x => x.Key == nameof(MapProposal.Achievements));
    }

    [Test]
    public async Task ConcurrentSubmissions_ShouldOnlyApplyOnce()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = new MapProposalService(firstContext, new FixedTimeProvider());
        var second = new MapProposalService(secondContext, new FixedTimeProvider());

        var results = await Task.WhenAll(
            first.SubmitAsync(new(1), new(1), new(1), CancellationToken.None),
            second.SubmitAsync(new(1), new(1), new(1), CancellationToken.None));

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

        var result = await new MapProposalService(db, new FixedTimeProvider())
            .SubmitAsync(new(1), new(1), new(1), CancellationToken.None);

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
            draft.Rating.DiffStar = new(6);

        var service = new MapProposalService(db, new FixedTimeProvider());
        var submit = () => service.SubmitAsync(new(1), new(1), new(1), CancellationToken.None);

        await submit.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Save or discard pending proposal changes before transitioning it.");
        db.ChangeTracker.HasChanges().Should().BeTrue();
        var persisted = await db.MapProposals.SingleAsync();
        persisted.Should().BeOfType<DraftProposal>();
        persisted.Rating.DiffStar.Value.Should().Be(5);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => _transitionAt;
    }

    // Each test owns a new database. The supplied server must allow creating test databases.
    private sealed class TestDatabase(DbContextOptions<ServerDbContext> options) : IAsyncDisposable
    {
        public ServerDbContext Context { get; } = new(options);
        public ServerDbContext CreateContext() => new(options);

        public static async Task<TestDatabase> CreateAsync()
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
                await SeedAsync(database.Context);
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try { await Context.Database.EnsureDeletedAsync(); }
            finally { await Context.DisposeAsync(); }
        }

        private static async Task SeedAsync(ServerDbContext db)
        {
            db.AddRange(
                new Guild
                {
                    Id = new(1), Info = GuildInfo.TryCreate("Test Guild", "TEST", "Test guild description", Color.White, _createdAt).Value,
                    Requirements = default, Status = Guild.EGuildStatus.Unverified, DiscordInfo = default
                },
                new Context { Id = new(1), GuildId = new(1), Info = new ContextInfo("Main", "") },
                new Context { Id = new(2), GuildId = new(1), Info = new ContextInfo("Other", "") },
                new Player
                {
                    Id = new(1), Info = new PlayerInfo("Author", "", "", _createdAt), HardwareInfo = default,
                    LinkedAccounts = default, SubscriptionInfo = default
                },
                new GameMode { Id = new(1), Name = "Standard" },
                new PlayMode { Id = new(1), Name = "Standard" },
                new Song
                {
                    Id = new(1), Hash = SongHash.CreateUnsafe(new string('a', 40)).Value, UploadedAt = _createdAt,
                    Info = new SongInfo("Test", "Test", "", "Author", "Mapper")
                },
                new SongDifficulty
                {
                    Id = new(1), SongId = new(1), GameModeId = new(1),
                    Stats = new SongDifficultyStats(MaxScore.CreateUnsafe(115).Value, NJS.CreateUnsafe(10).Value,
                        1, 0, 0, 1, 1)
                },
                new Category { Id = new(1), GuildId = new(1), Info = CategoryInfo.TryCreate("Category", "Test category").Value },
                new RankedMapListAchievement(new(1), new(1), new(1), null,
                    new AchievementInfo("Achievement", Color.White), default, null, false, default, 1),
                new MapRequestThread { Id = new(1), ContextId = new(1), AuthorId = new(1), CreatedAt = _createdAt },
                new DraftProposal
                {
                    Id = new(1), ThreadId = new(1), AuthorId = new(1), CreatedAt = _createdAt,
                    Rating = new RankedMapRating { AccStar = new(2), DiffStar = new(5) },
                    Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
                    MapVersions =
                    [new MapProposalVersion { SongDifficultyId = new(1), PlayModeId = new(1), PlayModeName = "Standard" }],
                    Categories = [new MapProposalCategory { CategoryId = new(1), Name = "Category" }],
                    Achievements = [new MapProposalAchievement { AchievementId = new(1), Name = "Achievement" }]
                });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }
}
