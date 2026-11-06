using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedScores;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedScores;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class ScoreWriteConcurrencyTests
{
    private readonly RankedMapId _map = new(1);
    private readonly Point.PointId _point = new(3);

    private sealed class UnusedQueue : IHeavyBackgroundTaskQueue
    {
        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
            => throw new InvalidOperationException("This fixture uses an editable curve with no publication.");

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken token)
            => throw new NotSupportedException();
    }

    private sealed class WritePause : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("UPDATE \"RankedScores\"", StringComparison.Ordinal)) return result;

            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }

    private sealed class LockProbe : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CandidateReads { get; private set; }
        public int RankWrites { get; private set; }
        public bool UsedTransaction { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UsedTransaction |= eventData.Context?.Database.CurrentTransaction is not null;

            if (command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal))
                Entered.TrySetResult();

            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            UsedTransaction |= eventData.Context?.Database.CurrentTransaction is not null;

            if (command.CommandText.Contains("FROM \"RankedScores\"", StringComparison.Ordinal))
                CandidateReads++;

            return ValueTask.FromResult(result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("DENSE_RANK()", StringComparison.Ordinal)) RankWrites += result;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RankFailure(string sqlState) : DbCommandInterceptor
    {
        public int Attempts { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("DENSE_RANK()", StringComparison.Ordinal) || ++Attempts != 1)
                return result;

            await using var failure = command.Connection!.CreateCommand();
            failure.Transaction = command.Transaction;
            failure.CommandText =
                $"DO $$ BEGIN RAISE EXCEPTION 'Retry score writes' USING ERRCODE = '{sqlState}'; END $$;";
            await failure.ExecuteNonQueryAsync(cancellationToken);
            return result;
        }
    }

    private static ServiceProvider Services(
        PointCurveTestDatabase database, DbCommandInterceptor? interceptor = null, bool retry = false)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHeavyBackgroundTaskQueue>(new UnusedQueue())
            .AddScoped(_ => database.CreateContext(interceptor, retry))
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .BuildServiceProvider();

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task Recalculation_ShouldWaitBeforeReadingCandidatesAndPreserveAConcurrentConfirmation()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.Context.RankedMaps.ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Requirements.NeedConfirmation, true));
        var raw = await database.AddScoreAsync(1, 600);
        await using var initial = Services(database);
        await using var initialScope = initial.CreateAsyncScope();
        await initialScope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(raw, token: CancellationToken.None);

        var pause = new WritePause();
        var probe = new LockProbe();
        await using var writer = Services(database, pause);
        await using var reader = Services(database, probe);
        await using var writerScope = writer.CreateAsyncScope();
        await using var readerScope = reader.CreateAsyncScope();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var writing = writerScope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: timeout.Token);
        Task<bool>? reading = null;
        Task<int>? confirming = null;

        try
        {
            await pause.Entered.Task.WaitAsync(timeout.Token);
            reading = readerScope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
                .RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: timeout.Token);

            await probe.Entered.Task.WaitAsync(timeout.Token);
            probe.CandidateReads.Should().Be(0);
            reading.IsCompleted.Should().BeFalse();

            confirming = RankedScoreService.UpdateRankedScoreConfirmationStateAsync(
                initialScope.ServiceProvider.GetRequiredService<ServerDbContext>(), TimeProvider.System,
                new ContextId(2), raw.Id, RankedScore.ERankedScoreType.Accepted, timeout.Token);
        }
        finally
        {
            pause.Release.TrySetResult();
            await writing;
            if (reading is not null) await reading;
            if (confirming is not null) await confirming;
        }

        await initialScope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: timeout.Token);

        var selected = await database.Context.RankedScores.SingleAsync(x => x.IsSelected, timeout.Token);
        selected.Should().BeOfType<AcceptedRankedScore>().Which.Rank.Should().Be(1);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task ConcurrentScoreAndMapRefreshes_ShouldKeepOneSelectionAndConsistentRanks()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddPlayerAsync(2);
        var first = await database.AddScoreAsync(1, 600);
        var second = await database.AddScoreAsync(2, 800, playerId: 2);
        await using var provider = Services(database);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        await Task.WhenAll(Enumerable.Range(0, 12).Select(async index =>
        {
            await using var scope = provider.CreateAsyncScope();
            var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

            switch (index % 3)
            {
                case 0:
                    await scores.ExecuteAsync(index % 2 == 0 ? first : second, token: timeout.Token);
                    break;
                case 1:
                    await scores.RecalculatePlayerScoresForMapAsync(
                        _map, _point, new PlayerId(index % 2 + 1), token: timeout.Token);
                    break;
                case 2:
                    await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync([_map], db, token: timeout.Token);
                    break;
            }
        }));

        var selected = await database.Context.PointGivingRankedScores
            .Where(x => x.IsSelected)
            .OrderBy(x => x.Rank)
            .ToArrayAsync(timeout.Token);

        selected.Should().HaveCount(2);
        selected.Select(x => x.PlayerId.Value).Should().Equal(2, 1);
        selected.Select(x => x.Rank).Should().Equal(1, 2);
        (await database.Context.RankedScores.CountAsync(timeout.Token)).Should().Be(2);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task RankRefresh_ShouldSkipUnchangedRowsAndLeaveOtherMapsAvailable()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var raw = await database.AddScoreAsync(1, 600);
        var probe = new LockProbe();
        await using var provider = Services(database, probe);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(raw, token: CancellationToken.None);
        var previousWrites = probe.RankWrites;
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync([_map], db);
        probe.RankWrites.Should().Be(previousWrites);

        await using var blocker = database.CreateContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await RankedScoreUpdateRankPipeline.LockMapsAsync([_map], blocker, CancellationToken.None);

        await RankedScoreUpdateRankPipeline.UpdateRanksForRankedMapsAsync([new RankedMapId(2)], db)
            .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task UnrankedScore_ShouldAvoidTransactionsAndRankedScoreQueries()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var raw = await database.AddScoreAsync(1, 600);
        await database.Context.RankedMaps.Where(x => x.Id == _map).ExecuteDeleteAsync();
        var probe = new LockProbe();
        await using var provider = Services(database, probe);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(raw, token: CancellationToken.None);

        probe.UsedTransaction.Should().BeFalse();
        probe.CandidateReads.Should().Be(0);
        probe.Entered.Task.IsCompleted.Should().BeFalse();
    }

    [Test]
    [Arguments(PostgresErrorCodes.SerializationFailure)]
    [Arguments(PostgresErrorCodes.DeadlockDetected)]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task RankingFailure_ShouldRollbackCandidateWritesAndRetryWithoutDuplicates(string sqlState)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var raw = await database.AddScoreAsync(1, 600);
        var failure = new RankFailure(sqlState);
        await using var provider = Services(database, failure, retry: true);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(raw, token: CancellationToken.None);

        failure.Attempts.Should().Be(2);
        var selected = await database.Context.PointGivingRankedScores.SingleAsync();
        selected.RankedMapId.Should().Be(_map);
        selected.IsSelected.Should().BeTrue();
        selected.Rank.Should().Be(1);
        (await database.Context.Scores.CountAsync()).Should().Be(1);
        scope.ServiceProvider.GetRequiredService<ServerDbContext>().ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task WaitingScore_ShouldUseVersionsAndRequirementsCommittedByARevision(bool removeVersion)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var raw = await database.AddScoreAsync(1, 600);
        var probe = new LockProbe();
        await using var provider = Services(database, probe);
        await using var scope = provider.CreateAsyncScope();
        await using var blocker = database.CreateContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await RankedScoreUpdateRankPipeline.LockMapsAsync([_map], blocker, timeout.Token);
        var waiting = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(raw, token: timeout.Token);

        try
        {
            await probe.Entered.Task.WaitAsync(timeout.Token);

            if (removeVersion)
                await blocker.MapVersions.Where(x => x.RankedMapId == _map).ExecuteDeleteAsync(timeout.Token);
            else
                await blocker.RankedMaps.Where(x => x.Id == _map).ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Requirements.NeedConfirmation, true), timeout.Token);

            await transaction.CommitAsync(timeout.Token);
        }
        finally
        {
            await transaction.DisposeAsync();
            await waiting;
        }

        if (removeVersion)
            (await database.Context.RankedScores.CountAsync(timeout.Token)).Should().Be(0);
        else
            (await database.Context.RankedScores.SingleAsync(timeout.Token)).Should().BeOfType<PendingRankedScore>();
    }
}
