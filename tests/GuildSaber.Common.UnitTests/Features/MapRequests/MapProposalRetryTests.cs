using System.Data.Common;
using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Api.Features.MapRequests.Http;
using GuildSaber.Api.Features.MapRequests.Services;
using GuildSaber.Api.Features.MapRequests.Settings;
using GuildSaber.Api.Features.RankedMaps.Http;
using GuildSaber.Api.Features.Songs;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.BeatSaver;
using GuildSaber.Common.Services.BeatSaver.Models.StrongTypes;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.Scores;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;
using ReviewResponse = GuildSaber.Api.Features.MapRequests.Services.MapProposalReviewService.ReviewResponse;

namespace GuildSaber.Common.UnitTests.Features.MapRequests;

public partial class MapProposalServiceTests
{
    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task Submission_ShouldResolveRatingOutsideTheTransactionAndReuseItOnRetry(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync(new MapProposalRating
        {
            AccStar = new RankedMapRating.AccuracyStar(2)
        });
        var failure = new TransientCommandFailure("UPDATE \"MapProposals\" AS proposal", sqlState);
        await using var db = database.CreateRetryingContext(failure);
        using var handler = new RatingHandler(_ =>
        {
            db.Database.CurrentTransaction.Should().BeNull();
            return JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse()));
        });
        using var client = new HttpClient(handler);

        (await CreateService(db, new BeatLeaderApi(client)).SubmitAsync(
                new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Success>();

        failure.Attempts.Should().Be(2);
        handler.RequestCount.Should().Be(1);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task Submission_ShouldRejectRatingInputsChangedWhileExMachinaWasRunning()
    {
        await using var database = await TestDatabase.CreateAsync(new MapProposalRating
        {
            AccStar = new RankedMapRating.AccuracyStar(2)
        });
        using var handler = new RatingHandler(_ =>
        {
            database.Context.Database.CurrentTransaction.Should().BeNull();
            using var other = database.CreateContext();
            other.MapProposals.ExecuteUpdate(setters => setters.SetProperty(
                x => x.Requirements.MandatoryModifiers, AbstractScore.EModifiers.FasterSong));

            return JsonResponse(JsonSerializer.Serialize(CreateExMachinaResponse()));
        });
        using var client = new HttpClient(handler);

        (await CreateService(database.Context, new BeatLeaderApi(client)).SubmitAsync(
                new ContextId(1), new MapProposalId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Conflict>();

        (await database.Context.MapProposals.SingleAsync()).Should().BeOfType<DraftProposal>();
    }

    private sealed class ProposalQuotaQueryCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT count(", StringComparison.Ordinal)
                && command.CommandText.Contains("FROM \"MapProposals\"", StringComparison.Ordinal))
                Count++;

            return ValueTask.FromResult(result);
        }
    }

    private sealed class TransientCommandFailure(string statement, string sqlState) : DbCommandInterceptor
    {
        public int Attempts { get; private set; }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await FailOnceAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await FailOnceAsync(command, cancellationToken);
            return result;
        }

        private async Task FailOnceAsync(DbCommand command, CancellationToken token)
        {
            if (!command.CommandText.Contains(statement, StringComparison.Ordinal)) return;

            if (++Attempts != 1) return;

            // Raise the real PostgreSQL error inside the transaction, leaving it aborted until rollback.
            await using var failure = command.Connection!.CreateCommand();
            failure.Transaction = command.Transaction;
            failure.CommandText = $"""
                                   DO $$ BEGIN
                                       RAISE EXCEPTION 'Transient proposal regression failure' USING ERRCODE = '{sqlState}';
                                   END $$;
                                   """;

            await failure.ExecuteNonQueryAsync(token);
        }
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    public async Task Submission_ShouldRetryTheTransactionAfterTransientFailure(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync();
        var failure = new TransientCommandFailure("UPDATE \"MapProposals\" AS proposal", sqlState);
        await using var db = database.CreateRetryingContext(failure);

        (await CreateService(db).SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                CancellationToken.None))
            .Should().BeOfType<MapProposalService.TransitionResponse.Success>();

        failure.Attempts.Should().Be(2);
        (await database.Context.MapProposals.OfType<SubmittedProposal>().CountAsync()).Should().Be(1);
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    public async Task Acceptance_ShouldRetryRolledBackMapInsertsAndQueueOneRefresh(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateService(database.Context).SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);
        var failure = new TransientCommandFailure("INSERT INTO \"CategoryRankedMap\"", sqlState);
        await using var db = database.CreateRetryingContext(failure);
        var queue = new RecordingQueue();

        (await CreateReviewService(db, queue).AcceptAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                CancellationToken.None, bypassVoteRequirements: true))
            .Should().BeOfType<ReviewResponse.Accepted>();

        failure.Attempts.Should().Be(2);
        queue.Count.Should().Be(1);
        (await database.Context.MapProposals.OfType<AcceptedProposal>().CountAsync()).Should().Be(1);
        (await database.Context.MapVersions.CountAsync()).Should().Be(1);

        var map = await database.Context.RankedMaps
            .Include(x => x.Categories)
            .SingleAsync();

        map.Categories.Should().ContainSingle();
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    public async Task Vote_ShouldRetryTheTransactionAfterTransientFailure(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateService(database.Context).SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);
        var failure = new TransientCommandFailure("INSERT INTO \"MapProposalVotes\"", sqlState);
        await using var db = database.CreateRetryingContext(failure);
        var votes = new MapProposalVoteService(db, new FixedTimeProvider());

        (await votes.SetCommunityAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1), true,
                CancellationToken.None))
            .Should().BeOfType<MapProposalVoteService.VoteResponse.Success>();

        failure.Attempts.Should().Be(2);
        (await database.Context.MapProposalVotes.CountAsync()).Should().Be(1);
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    public async Task Boost_ShouldRetryTheTransactionAfterTransientFailure(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateService(database.Context).SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
            CancellationToken.None);
        var failure = new TransientCommandFailure("INSERT INTO \"MapRequestThreadBoosts\"", sqlState);
        await using var db = database.CreateRetryingContext(failure);
        var boosts = new MapRequestBoostService(db, Options.Create(new MapRequestBoostSettings()));

        (await boosts.AddAsync(new ContextId(1), new MapRequestThreadId(1), new PlayerId(1), CancellationToken.None))
            .Should().BeOfType<MapRequestBoostService.BoostResponse.Success>();

        failure.Attempts.Should().Be(2);
        (await database.Context.MapRequestThreadBoosts.CountAsync()).Should().Be(1);
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    public async Task DraftCreation_ShouldRetryTransientFailureBeforeCheckingContextExistence(string sqlState)
    {
        await using var database = await TestDatabase.CreateAsync();
        var failure = new TransientCommandFailure("FROM \"Contexts\"", sqlState);
        await using var db = database.CreateRetryingContext(failure);
        var beatLeader = new BeatLeaderApi(_unusedHttpClient);
        var songService = new SongService(db, beatLeader, new ScoreSaberApi(_unusedHttpClient));
        var drafts = new MapProposalDraftService(db, new BeatSaverApi(_unusedHttpClient), songService,
            new FixedTimeProvider(), Options.Create(new MapRequestSettings()));
        var request = new MapProposalRequests.CreateDraft(
            [
                new MapProposalRequests.MapVersionInput(
                    new MapProposalRequests.SongReference.ByHash(SongHash.CreateUnsafe(new string('a', 40)).Value),
                    "Standard", EDifficulty.ExpertPlus, "Standard", 0)
            ],
            new RankedMapRequests.RankedMapRequirements(), new RankedMapRequests.ManualRating(5, 2), [], []);

        (await drafts.CreateAsync(new ContextId(999), null, new PlayerId(1), request, CancellationToken.None))
            .Should().BeOfType<MapProposalDraftService.CreateResponse.ContextNotFound>();

        failure.Attempts.Should().Be(2);
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 0)]
    public async Task Submission_ShouldOnlyCheckPlayerQuotasForPlayerAuthoredProposals(
        bool imported, int expectedQuotaQueries)
    {
        await using var database = await TestDatabase.CreateAsync();

        if (imported)
        {
            await database.Context.MapRequestThreads.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.AuthorId, (PlayerId?)null)
                .SetProperty(x => x.IsLegacyImport, true));
            await database.Context.MapProposals.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.AuthorId, (PlayerId?)null));
        }

        var queries = new ProposalQuotaQueryCounter();
        await using var db = database.CreateRetryingContext(queries);
        var service = CreateService(db);

        var result = imported
            ? await service.SubmitImportedAsync(new ContextId(1), new MapProposalId(1), CancellationToken.None)
            : await service.SubmitAsync(new ContextId(1), new MapProposalId(1), new PlayerId(1),
                CancellationToken.None);

        result.Should().BeOfType<MapProposalService.TransitionResponse.Success>();
        queries.Count.Should().Be(expectedQuotaQueries);
    }
}
