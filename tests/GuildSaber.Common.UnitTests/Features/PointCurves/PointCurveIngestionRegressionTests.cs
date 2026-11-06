using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedScores.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.Services.BeatLeader.Models.StrongTypes;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.Scores;
using GuildSaber.Database.Models.StrongTypes;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using EModifiers = GuildSaber.Database.Models.Server.Scores.AbstractScore.EModifiers;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveIngestionRegressionTests
{
    private static readonly RankedMapId _map = new(1);
    private static readonly Point.PointId _point = new(3);

    private sealed class Queue : IHeavyBackgroundTaskQueue
    {
        private readonly Queue<Func<CancellationToken, ValueTask>> _items = new();

        public int Count => _items.Count;

        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
        {
            _items.Enqueue(workItem);

            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(_items.Dequeue());

        public async Task DrainAsync()
        {
            while (_items.TryDequeue(out var work))
                await work(CancellationToken.None);
        }
    }

    private sealed class Commands : DbCommandInterceptor
    {
        public int Writes { get; private set; }
        public int BenchmarkReads { get; private set; }
        public int RankUpdates { get; private set; }

        public void Reset() => (Writes, BenchmarkReads, RankUpdates) = (0, 0, 0);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            // SaveChanges also executes writes through a reader when retrieving generated values.
            if (command.CommandText.Contains("UPDATE ", StringComparison.Ordinal)
                || command.CommandText.Contains("INSERT INTO ", StringComparison.Ordinal)
                || command.CommandText.Contains("DELETE FROM ", StringComparison.Ordinal))
                Writes++;

            if (command.CommandText.Contains("ROW_NUMBER() OVER", StringComparison.Ordinal)
                && command.CommandText.Contains("PARTITION BY ranked.\"PlayerId\"", StringComparison.Ordinal))
                BenchmarkReads++;

            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Writes++;

            if (command.CommandText.Contains("DENSE_RANK()", StringComparison.Ordinal))
                RankUpdates++;

            return ValueTask.FromResult(result);
        }
    }

    private static ServiceProvider Services(PointCurveTestDatabase database, Queue queue, Commands commands)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHeavyBackgroundTaskQueue>(queue)
            .AddScoped(_ => database.CreateContext(commands))
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .AddTransient<MapPointCurvePipeline>()
            .AddTransient<RankedScoreConfirmationPipeline>()
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .BuildServiceProvider();

    private static async Task SeedAsync(PointCurveTestDatabase database, IServiceProvider services)
    {
        await database.Context.Database.ExecuteSqlRawAsync("""
                                                           UPDATE "Points"
                                                           SET "CurveSettings_MapCurvePolicy" = 2,
                                                               "CurveSettings_RunnerUpRewardFractions" = ARRAY[0.8,0.6,0.4]::double precision[]
                                                           """);

        var scores = services.GetRequiredService<ScoreAddOrUpdatePipeline>();

        for (var player = 1; player <= 5; player++)
        {
            if (player > 1) await database.AddPlayerAsync(player);

            await database.AddScoreAsync(player, 900 - player * 100, playerId: player, isWin: true);
            await scores.RecalculatePlayerScoresForMapAsync(
                _map, _point, new PlayerId(player), token: CancellationToken.None);
        }

        await services.GetRequiredService<PointCurveService>().RefreshAsync(_map, _point, CancellationToken.None);
        await services.GetRequiredService<MapPointCurvePipeline>().ExecuteAsync(_map, _point, regenerate: false);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CancelledScore_ShouldNotPersistRawOrRankedScores(bool beatLeader)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var score = await database.AddScoreAsync(1, 600, beatLeader: beatLeader);

        await database.Context.Scores.ExecuteDeleteAsync();

        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        Func<Task> ingest = () => scores.ExecuteAsync(score, token: cancellation.Token);

        await ingest.Should().ThrowAsync<OperationCanceledException>();

        commands.Writes.Should().Be(0);
        (await database.Context.Scores.AnyAsync()).Should().BeFalse();
        (await database.Context.RankedScores.AnyAsync()).Should().BeFalse();
        queue.Count.Should().Be(0);
    }

    [Test]
    public async Task NewScoreFollowedByReplay_ShouldStoreOneRawScoreAndOneRankedCandidate()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var score = await database.AddScoreAsync(1, 600);

        await database.Context.Scores.ExecuteDeleteAsync();

        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        await scores.ExecuteAsync(score, token: CancellationToken.None);
        await scores.ExecuteAsync(score, token: CancellationToken.None);

        (await database.Context.Scores
            .CountAsync()).Should().Be(1);

        var rankedScore = await database.Context.ScoredRankedScores
            .SingleAsync();

        rankedScore.ScoreId.Should().Be(score.Id);
        rankedScore.IsSelected.Should().BeTrue();
        ((float)rankedScore.RawPoints).Should().Be(115);
    }

    [Test]
    [Arguments(1000, 641, 640)]
    [Arguments(20_000_000, 19_000_001, 19_000_000)]
    public async Task PreciseAccuracies_ShouldMatchSqlBenchmarksAndSkipUnchangedCurveWork(
        int maxScore, int leaderScore, int runnerUpScore)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var maximum = MaxScore.TryCreate(maxScore).Value;

        await database.AddPlayerAsync(2);
        await database.Context.SongDifficulties
            .ExecuteUpdateAsync(x => x.SetProperty(d => d.Stats.MaxScore, maximum));
        await database.Context.Database.ExecuteSqlRawAsync("""
                                                           UPDATE "Points"
                                                           SET "CurveSettings_MapCurvePolicy" = 2,
                                                               "CurveSettings_RunnerUpRewardFractions" = ARRAY[0.8]::double precision[]
                                                           """);

        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var leader = await database.AddScoreAsync(1, leaderScore);
        var runnerUp = await database.AddScoreAsync(2, runnerUpScore, playerId: 2);

        await scores.ExecuteAsync(leader, token: CancellationToken.None);
        await scores.ExecuteAsync(runnerUp, token: CancellationToken.None);
        await queue.DrainAsync();

        var curve = await database.Context.RankedMapPointCurves
            .SingleAsync();

        curve.BenchmarkAccuracies.Should().Equal(
            Accuracy.From(leader.BaseScore, maximum).Value / 100d,
            Accuracy.From(runnerUp.BaseScore, maximum).Value / 100d);
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(1) && x.IsSelected)).RawPoints).Should().Be(125);
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(2) && x.IsSelected)).RawPoints).Should().Be(120);

        commands.Reset();

        await scores.ExecuteAsync(leader, token: CancellationToken.None);

        queue.Count.Should().Be(0);
        commands.BenchmarkReads.Should().Be(0);
    }

    [Test]
    [Arguments(450)]
    [Arguments(500)]
    public async Task ScoreOutsideBenchmarks_ShouldNotQueueCurveWorkOrReadTheLeaderboard(int baseScore)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);

        var score = await database.AddScoreAsync(6, baseScore, playerId: 5);
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        commands.Reset();

        await scores.ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(0);
        commands.BenchmarkReads.Should().Be(0);
    }

    [Test]
    [Arguments(750, true)]
    [Arguments(800, false)]
    [Arguments(800, true)]
    public async Task UnchangedPersonalBest_ShouldNotQueueEvenForABenchmarkPlayer(int baseScore, bool beatLeader)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);

        var score = await database.AddScoreAsync(6, baseScore, beatLeader: beatLeader);
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        commands.Reset();

        for (var replay = 0; replay < 2; replay++)
            await scores.ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(0);
        commands.BenchmarkReads.Should().Be(0);
    }

    [Test]
    [Arguments(EModifiers.NoFail, true, 950)]
    [Arguments(EModifiers.None, false, 950)]
    [Arguments(EModifiers.None, true, 1200)]
    public async Task IneligibleAccuracy_ShouldNotQueueCurveWork(EModifiers modifiers, bool isWin, int baseScore)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);

        var score = await database.AddScoreAsync(6, baseScore, modifiers, playerId: 5, isWin: isWin);
        commands.Reset();

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(0);
        commands.BenchmarkReads.Should().Be(0);
    }

    [Test]
    [Arguments(1, 850)]
    [Arguments(5, 550)]
    public async Task NewBenchmark_ShouldRefreshTheCurveAndReweightOnce(int player, int baseScore)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);

        var score = await database.AddScoreAsync(6, baseScore, playerId: player);

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(1);
        commands.Reset();

        await queue.DrainAsync();

        commands.BenchmarkReads.Should().Be(1);
        commands.RankUpdates.Should().Be(1);
        var benchmarks = (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies;

        benchmarks.Should().Equal(player == 1 ? [0.85, 0.7, 0.6, 0.5] : [0.8, 0.7, 0.6, 0.55]);
        var reward = (float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(player) && x.IsSelected)).RawPoints;

        reward.Should().Be(player == 1 ? 125 : 110);
    }

    [Test]
    public async Task MissingOverride_ShouldBootstrapFromAnExistingScoreReplay()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);
        await database.Context.RankedMapPointCurves.ExecuteDeleteAsync();
        var score = await database.Context.Scores.SingleAsync(x => x.Id == new ScoreId(1));

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(1);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies
            .Should().Equal(0.8, 0.7, 0.6, 0.5);
    }

    [Test]
    [Arguments(false, EModifiers.None)]
    [Arguments(true, EModifiers.NoFail)]
    public async Task UpdatedBenchmark_ShouldRefreshWhenItBecomesIneligible(bool isWin, EModifiers modifiers)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);

        var original = await database.Context.BeatLeaderScores.SingleAsync(x => x.Id == new ScoreId(1));
        var updated = new BeatLeaderScore
        {
            Id = original.Id,
            BeatLeaderScoreId = BeatLeaderScoreId.CreateUnsafe(100),
            PlayerId = original.PlayerId,
            SongDifficultyId = original.SongDifficultyId,
            BaseScore = original.BaseScore,
            Modifiers = modifiers,
            SetAt = original.SetAt,
            MaxCombo = original.MaxCombo,
            IsFullCombo = original.IsFullCombo,
            MissedNotes = original.MissedNotes,
            BadCuts = original.BadCuts,
            Statistics = new ScoreStatistics
            {
                WinTracker = original.Statistics!.WinTracker with { IsWin = isWin },
                HitTracker = original.Statistics.HitTracker,
                AccuracyTracker = original.Statistics.AccuracyTracker,
                ScoreGraphTracker = original.Statistics.ScoreGraphTracker
            }
        };

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(updated, token: CancellationToken.None);

        queue.Count.Should().Be(1);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies
            .Should().Equal(0.7, 0.6, 0.5, 0.4);
    }

    [Test]
    public async Task UnfilledBenchmarks_ShouldIncludeALowerNewPlayer()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);
        await database.Context.ContextMembers
            .Where(x => x.PlayerId != new PlayerId(1))
            .ExecuteDeleteAsync();
        await scope.ServiceProvider.GetRequiredService<PointCurveService>()
            .RefreshAsync(_map, _point, CancellationToken.None);
        await database.AddPlayerAsync(6);
        var score = await database.AddScoreAsync(6, 400, playerId: 6);

        await scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>()
            .ExecuteAsync(score, token: CancellationToken.None);

        queue.Count.Should().Be(1);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies
            .Should().Equal(0.8, 0.4);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PendingOrRefusedScore_ShouldNotEnterBenchmarks(bool refused, bool replay)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);
        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(map => map.Requirements.NeedConfirmation, true));
        await database.Context.Database.ExecuteSqlRawAsync("UPDATE \"RankedScores\" SET \"Type\" = 3");
        var score = await database.AddScoreAsync(6, 950, playerId: 5);
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        if (refused)
        {
            await scores.RecalculatePlayerScoresForMapAsync(
                _map, _point, new PlayerId(5), token: CancellationToken.None);
            await database.Context.Database.ExecuteSqlRawAsync(
                "UPDATE \"RankedScores\" SET \"Type\" = 4 WHERE \"ScoreId\" = 6");
        }

        commands.Reset();
        await scores.ExecuteAsync(score, refreshCurveBenchmarks: replay, token: CancellationToken.None);

        queue.Count.Should().Be(0);
        commands.BenchmarkReads.Should().Be(0);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task AlreadySavedConfirmation_ShouldStillRefreshBenchmarks(bool accepted)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);
        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(map => map.Requirements.NeedConfirmation, true));
        await database.Context.Database.ExecuteSqlRawAsync("UPDATE \"RankedScores\" SET \"Type\" = 3");

        if (accepted)
        {
            await database.Context.Database.ExecuteSqlRawAsync(
                "UPDATE \"RankedScores\" SET \"Type\" = 2 WHERE \"PlayerId\" = 1");
            await scope.ServiceProvider.GetRequiredService<PointCurveService>()
                .RefreshAsync(_map, _point, CancellationToken.None);
        }

        var rankedScoreId = (await database.Context.RankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(1))).Id;

        await database.Context.Database.ExecuteSqlAsync($"""
                                                         UPDATE "RankedScores" SET "Type" = {(accepted ? 3 : 4)} WHERE "PlayerId" = 1
                                                         """);
        await scope.ServiceProvider.GetRequiredService<RankedScoreConfirmationPipeline>()
            .ExecuteAsync(new ContextId(2), rankedScoreId, CancellationToken.None);

        queue.Count.Should().Be(1);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies
            .Should().Equal(accepted ? [0.8, 0.7, 0.6, 0.5] : [0.7, 0.6, 0.5, 0.4]);
    }

    [Test]
    public async Task ExistingQueuedCheck_ShouldStopWithoutWritesWhenBenchmarksAreUnchanged()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        var commands = new Commands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();

        await SeedAsync(database, scope.ServiceProvider);
        await scope.ServiceProvider.GetRequiredService<PointCurveService>()
            .QueueRefreshAsync(_map, _point, CancellationToken.None);
        commands.Reset();

        await queue.DrainAsync();

        commands.BenchmarkReads.Should().Be(1);
        commands.Writes.Should().Be(0);
        commands.RankUpdates.Should().Be(0);
    }
}