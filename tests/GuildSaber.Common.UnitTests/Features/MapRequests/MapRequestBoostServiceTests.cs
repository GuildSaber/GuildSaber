using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Votes;
using GuildSaber.Database.Models.Server.Players;
using GuildSaber.Database.Models.Server.RankedMaps;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;
using Color = System.Drawing.Color;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;
using Response = GuildSaber.Api.Features.MapRequests.Services.MapRequestBoostService.BoostResponse;
using Tier = GuildSaber.Database.Models.Server.Players.PlayerSubscriptionInfo.ESubscriptionTier;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public class MapRequestBoostServiceTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private sealed class BoostSerializationFailureInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfBoostCommand(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfBoostCommand(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private static void ThrowIfBoostCommand(DbCommand command)
        {
            if (command.CommandText.Contains(nameof(ServerDbContext.MapRequestThreadBoosts), StringComparison.Ordinal))
                throw new PostgresException("Forced serialization failure", "ERROR", "ERROR",
                    PostgresErrorCodes.SerializationFailure);
        }
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

        public ServerDbContext CreateContext(params IInterceptor[] interceptors)
            => new(new DbContextOptionsBuilder<ServerDbContext>(options).AddInterceptors(interceptors).Options);

        public static async Task<TestDatabase> CreateAsync(Tier tier = Tier.Tier1)
        {
            var connectionString = Environment.GetEnvironmentVariable("GUILDSABER_TEST_POSTGRES");
            Skip.Unless(!string.IsNullOrWhiteSpace(connectionString),
                "Set GUILDSABER_TEST_POSTGRES to an isolated PostgreSQL server to run persistence tests.");
            var connection = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = $"guildsaber_boost_test_{Guid.NewGuid():N}", Pooling = false
            };
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>()
                .UseNpgsql(connection.ConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            optionsBuilder.WithExpressionExpanding();
            var database = new TestDatabase(optionsBuilder.Options);
            try
            {
                await database.Context.Database.EnsureCreatedAsync();
                var db = database.Context;
                db.AddRange(
                    new Guild
                    {
                        Id = new GuildId(1),
                        Info = GuildInfo.TryCreate("Test Guild", "TEST", "Test guild description", Color.White,
                            _createdAt).Value,
                        Requirements = default, Status = Guild.EGuildStatus.Unverified, DiscordInfo = default
                    },
                    new Context { Id = new ContextId(1), GuildId = new GuildId(1), Info = new ContextInfo("Main", "") },
                    new Context
                    {
                        Id = new ContextId(2), GuildId = new GuildId(1), Info = new ContextInfo("Other", "")
                    });
                for (var id = 1; id <= 2; id++)
                    db.Players.Add(new Player
                    {
                        Id = new PlayerId(id), Info = new PlayerInfo("Supporter", "", "", _createdAt),
                        Platform = default,
                        LinkedAccounts = default, SubscriptionInfo = new PlayerSubscriptionInfo(tier)
                    });
                for (var id = 1; id <= 4; id++)
                {
                    db.MapRequestThreads.Add(new MapRequestThread
                    {
                        Id = new MapRequestThreadId(id), ContextId = new ContextId(id == 3 ? 2 : 1),
                        AuthorId = new PlayerId(1), CreatedAt = _createdAt
                    });
                    db.MapProposals.Add(CreateSubmitted(id, id, 1));
                }

                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
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
    public async Task AddMoveRemove_ShouldReuseAllowanceWithoutKeepingHistory()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
        (await service.MoveAsync(new ContextId(1), new MapRequestThreadId(1), new MapRequestThreadId(2),
                new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
        var allocation = await database.Context.MapRequestThreadBoosts.SingleAsync();
        allocation.PlayerId.Should().Be(new PlayerId(1));
        allocation.ThreadId.Should().Be(new MapRequestThreadId(2));

        (await service.RemoveAsync(new ContextId(1), new MapRequestThreadId(2), new PlayerId(1),
                CancellationToken.None)).Should()
            .BeOfType<Response.Success>();
        (await database.Context.MapRequestThreadBoosts.AnyAsync()).Should().BeFalse();
        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
    }

    [Test]
    [Arguments(Tier.None, 0)]
    [Arguments(Tier.Tier1, 1)]
    [Arguments(Tier.Tier2, 2)]
    [Arguments(Tier.Tier3, 3)]
    public async Task Allowance_ShouldUseConfiguredSubscriptionTier(Tier tier, int limit)
    {
        await using var database = await TestDatabase.CreateAsync(tier);
        var settings = new MapRequestBoostSettings { None = 0, Tier1 = 1, Tier2 = 2, Tier3 = 3 };
        var service = new MapRequestBoostService(database.Context, Options.Create(settings));
        for (var id = 1; id <= limit; id++)
            (await service.AddAsync(new ContextId(id == 3 ? 2 : 1), new MapRequestThreadId(id), new PlayerId(1),
                    CancellationToken.None))
                .Should().BeOfType<Response.Success>();

        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(4), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.LimitReached>().Which.Limit.Should().Be(limit);
        (await database.Context.MapRequestThreadBoosts.CountAsync()).Should().Be(limit);
    }

    [Test]
    public async Task Allowance_ShouldBeSharedAcrossContextsButAllowTransferringAtTheLimit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);
        await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);

        (await service.AddAsync(new ContextId(2), new MapRequestThreadId(3), new PlayerId(1), CancellationToken.None))
            .Should()
            .BeOfType<Response.LimitReached>();
        (await service.MoveAsync(new ContextId(2), new MapRequestThreadId(1), new MapRequestThreadId(3),
                new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
        (await database.Context.MapRequestThreadBoosts.SingleAsync()).ThreadId.Should().Be(new MapRequestThreadId(3));
    }

    [Test]
    public async Task RepeatedAdd_ShouldBeIdempotentAndMoveToAnAlreadyBoostedThreadShouldNotSpendTheSource()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context, 2);
        await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);
        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
        (await service.MoveAsync(new ContextId(1), new MapRequestThreadId(1), new MapRequestThreadId(1),
                new PlayerId(1), CancellationToken.None)).Should()
            .BeOfType<Response.Success>();
        await service.AddAsync(new ContextId(1), new MapRequestThreadId(2), new PlayerId(1), CancellationToken.None);

        (await service.MoveAsync(new ContextId(1), new MapRequestThreadId(1), new MapRequestThreadId(2),
                new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<Response.AlreadyBoosted>();
        (await database.Context.MapRequestThreadBoosts.CountAsync()).Should().Be(2);
    }

    [Test]
    public async Task WrongPlayerOrContext_ShouldNotMoveOrRemoveAnotherAllocation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);
        await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);

        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(99), CancellationToken.None))
            .Should()
            .BeOfType<Response.PlayerNotFound>();
        (await service.AddAsync(new ContextId(2), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None))
            .Should()
            .BeOfType<Response.ThreadNotFound>();
        (await service.AddAsync(new ContextId(1), new MapRequestThreadId(99), new PlayerId(1), CancellationToken.None))
            .Should()
            .BeOfType<Response.ThreadNotFound>();
        (await service.MoveAsync(new ContextId(1), new MapRequestThreadId(1), new MapRequestThreadId(2),
                new PlayerId(2), CancellationToken.None))
            .Should().BeOfType<Response.SourceNotFound>();
        await service.RemoveAsync(new ContextId(2), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);
        await service.RemoveAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(2), CancellationToken.None);

        var allocation = await database.Context.MapRequestThreadBoosts.SingleAsync();
        allocation.PlayerId.Should().Be(new PlayerId(1));
        allocation.ThreadId.Should().Be(new MapRequestThreadId(1));
    }

    [Test]
    public async Task DraftOnlyThread_ShouldNotReceiveBoosts()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Context.MapProposals.Where(x => x.ThreadId == new MapRequestThreadId(1)).ExecuteDeleteAsync();
        database.Context.MapProposals.Add(CreateDraft(10, 1));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await CreateService(database.Context).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                CancellationToken.None))
            .Should().BeOfType<Response.NoSubmittedProposals>();
        (await database.Context.MapRequestThreadBoosts.AnyAsync()).Should().BeFalse();
    }

    [Test]
    public async Task ClosingLastSubmittedProposal_ShouldReleaseBoostsButKeepProposalVotesAndOtherThreads()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        db.AddRange(CreateSubmitted(5, 1, 2), CreateDraft(6, 1),
            new CommunityProposalVote
            {
                ProposalId = new MapProposalId(1), PlayerId = new PlayerId(2), IsPositive = true, VotedAt = _createdAt
            },
            new RankingTeamProposalVote
            {
                ProposalId = new MapProposalId(1), PlayerId = new PlayerId(2), IsPositive = false, VotedAt = _createdAt,
                Reason = "Needs another rating."
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var boosts = CreateService(db, 2);
        await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);
        await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(2), CancellationToken.None);
        await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(2), new PlayerId(1), CancellationToken.None);
        using var client = new HttpClient();
        var proposals = new MapProposalService(db, TimeProvider.System, new BeatLeaderApi(client),
            Options.Create(new MapRequestSettings()));

        (await proposals.CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), "Withdrawn",
                CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Success>();
        (await db.MapRequestThreadBoosts.CountAsync()).Should().Be(3);
        var trackedBoosts = await db.MapRequestThreadBoosts.AsTracking()
            .Where(x => x.ThreadId == new MapRequestThreadId(1)).ToArrayAsync();

        (await proposals.CloseAsync(new ContextId(1), new MapProposalId(5), new PlayerId(2), "Withdrawn",
                CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Success>();

        (await db.MapRequestThreadBoosts.SingleAsync()).ThreadId.Should().Be(new MapRequestThreadId(2));
        trackedBoosts.Should().OnlyContain(x => db.Entry(x).State == EntityState.Detached);
        (await db.MapProposalVotes.CountAsync()).Should().Be(2);
        (await db.MapProposalVotes.OfType<RankingTeamProposalVote>().SingleAsync()).Reason.Should()
            .Be("Needs another rating.");
        (await db.MapProposals.OfType<DraftProposal>().CountAsync()).Should().Be(1);
        (await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(2), CancellationToken.None))
            .Should().BeOfType<Response.NoSubmittedProposals>();

        db.MapProposals.Add(CreateSubmitted(7, 1, 1));
        await db.SaveChangesAsync();
        (await db.MapRequestThreadBoosts.AnyAsync(x => x.ThreadId == new MapRequestThreadId(1))).Should().BeFalse();
        (await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(2), CancellationToken.None))
            .Should().BeOfType<Response.Success>();
    }

    [Test]
    public async Task ConcurrentAllocations_ShouldNotExceedTheAllowance()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();

        var results = await Task.WhenAll(
            CreateService(first).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                CancellationToken.None),
            CreateService(second).AddAsync(new ContextId(1), new MapRequestThreadId(2), new PlayerId(1),
                CancellationToken.None));

        results.Count(x => x is Response.Success).Should().Be(1);
        results.Count(x => x is Response.Conflict or Response.LimitReached).Should().Be(1);
        (await database.Context.MapRequestThreadBoosts.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task ConcurrentClosureAndAllocation_ShouldNotLeaveAnInactiveThreadBoosted()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var boostContext = database.CreateContext();
        await using var proposalContext = database.CreateContext();
        using var client = new HttpClient();
        var proposals = new MapProposalService(proposalContext, TimeProvider.System, new BeatLeaderApi(client),
            Options.Create(new MapRequestSettings()));

        await Task.WhenAll(
            CreateService(boostContext).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                CancellationToken.None),
            proposals.CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), "Withdrawn",
                CancellationToken.None));

        var hasSubmittedProposal = await database.Context.MapProposals.OfType<SubmittedProposal>()
            .AnyAsync(x => x.ThreadId == new MapRequestThreadId(1));
        var hasBoost = await database.Context.MapRequestThreadBoosts.AnyAsync();
        (hasSubmittedProposal || !hasBoost).Should().BeTrue();
    }

    [Test]
    public async Task SerializationFailureDuringAllocation_ShouldReturnConflict()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var failingContext = database.CreateContext(new BoostSerializationFailureInterceptor());

        (await CreateService(failingContext).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
                CancellationToken.None))
            .Should().BeOfType<Response.Conflict>();
        (await database.Context.MapRequestThreadBoosts.AnyAsync()).Should().BeFalse();
    }

    [Test]
    public async Task SerializationFailureDuringRelease_ShouldRollBackClosureAndPreserveBoosts()
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateService(database.Context).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
            CancellationToken.None);
        await using var failingContext = database.CreateContext(new BoostSerializationFailureInterceptor());
        using var client = new HttpClient();
        var proposals = new MapProposalService(failingContext, TimeProvider.System, new BeatLeaderApi(client),
            Options.Create(new MapRequestSettings()));

        (await proposals.CloseAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), "Withdrawn",
                CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Conflict>();

        (await database.Context.MapProposals.SingleAsync(x => x.Id == new MapProposalId(1)))
            .Should().BeOfType<SubmittedProposal>();
        (await database.Context.MapRequestThreadBoosts.SingleAsync()).ThreadId.Should().Be(new MapRequestThreadId(1));
    }

    [Test]
    public async Task MovingTrackedAllocation_ShouldDetachOldIdentityAndNotSaveUnrelatedChanges()
    {
        await using var database = await TestDatabase.CreateAsync();
        var db = database.Context;
        var service = CreateService(db);
        await service.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None);
        var allocation = await db.MapRequestThreadBoosts.AsTracking().SingleAsync();
        var player = await db.Players.AsTracking().SingleAsync(x => x.Id == new PlayerId(1));
        player.IsManager = true;

        (await service.MoveAsync(new ContextId(1), new MapRequestThreadId(1), new MapRequestThreadId(2),
                new PlayerId(1), CancellationToken.None)).Should()
            .BeOfType<Response.Success>();

        db.Entry(allocation).State.Should().Be(EntityState.Detached);
        db.Entry(player).State.Should().Be(EntityState.Modified);
        await using var verification = database.CreateContext();
        (await verification.Players.SingleAsync(x => x.Id == player.Id)).IsManager.Should().BeFalse();
    }

    [Test]
    public async Task PendingBoostChanges_ShouldNotBeDiscardedOrBypassQuota()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.MapRequestThreadBoosts.Add(new MapRequestThreadBoost
            { PlayerId = new PlayerId(1), ThreadId = new MapRequestThreadId(1) });
        var service = CreateService(database.Context);

        var add = ()
            => service.AddAsync(new ContextId(1), new MapRequestThreadId(2), new PlayerId(1), CancellationToken.None);
        await add.Should().ThrowAsync<InvalidOperationException>();
        database.Context.ChangeTracker.HasChanges().Should().BeTrue();
        (await database.Context.MapRequestThreadBoosts.AnyAsync()).Should().BeFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task DeletingEitherParent_ShouldDeleteAllocations(bool deletePlayer)
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateService(database.Context).AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1),
            CancellationToken.None);

        if (deletePlayer) await database.Context.Players.Where(x => x.Id == new PlayerId(1)).ExecuteDeleteAsync();
        else
            await database.Context.MapRequestThreads.Where(x => x.Id == new MapRequestThreadId(1)).ExecuteDeleteAsync();

        (await database.Context.MapRequestThreadBoosts.AnyAsync()).Should().BeFalse();
        if (deletePlayer) (await database.Context.MapRequestThreads.CountAsync()).Should().Be(4);
    }

    private static MapRequestBoostService CreateService(ServerDbContext db, int limit = 1)
        => new(db, Options.Create(new MapRequestBoostSettings { Tier1 = limit }));

    private static SubmittedProposal CreateSubmitted(long id, long threadId, int playerId) => new()
    {
        Id = new MapProposalId(id), ThreadId = new MapRequestThreadId(threadId), AuthorId = new PlayerId(playerId),
        CreatedAt = _createdAt,
        SubmittedAt = _createdAt,
        Requirements = new RankedMapRequirements(false, false, null, 0, 0, null),
        Rating = new MapProposalRating
            { AccStar = new RankedMapRating.AccuracyStar(1), DiffStar = new RankedMapRating.DifficultyStar(1) }
    };

    private static DraftProposal CreateDraft(long id, long threadId) => new()
    {
        Id = new MapProposalId(id), ThreadId = new MapRequestThreadId(threadId), AuthorId = new PlayerId(1),
        CreatedAt = _createdAt,
        Requirements = new RankedMapRequirements(false, false, null, 0, 0, null), Rating = new MapProposalRating()
    };
}