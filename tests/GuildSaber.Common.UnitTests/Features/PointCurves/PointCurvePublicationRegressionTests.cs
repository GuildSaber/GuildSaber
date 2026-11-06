using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurvePublicationRegressionTests
{
    private static readonly ContextId _contextId = new(2);
    private static readonly RankedMapId _mapId = new(1);
    private static readonly Point.PointId _pointId = new(3);

    private sealed class RecordingQueue : IHeavyBackgroundTaskQueue
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
        {
            Interlocked.Increment(ref _count);

            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class OverrideReadGate(int expectedReaders) : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _readsCompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _continue =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _readers;

        public Task WaitForReadersAsync() => _readsCompleted.Task.WaitAsync(TimeSpan.FromSeconds(15));

        public void Release() => _continue.TrySetResult();

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("FROM \"RankedMapPointCurves\"", StringComparison.Ordinal)
                || Interlocked.Increment(ref _readers) > expectedReaders)
                return result;

            if (Volatile.Read(ref _readers) == expectedReaders)
                _readsCompleted.TrySetResult();

            await _continue.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);

            return result;
        }
    }

    private static ServiceProvider Services(PointCurveTestDatabase database, RecordingQueue queue)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHeavyBackgroundTaskQueue>(queue)
            .AddScoped(_ => database.CreateContext())
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .AddTransient<MapPointCurvePipeline>()
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .BuildServiceProvider();

    private static ServerDbContext ContextWithGate(PointCurveTestDatabase database, OverrideReadGate gate)
        => database.CreateContext(gate);

    private static PointCurveRequests.PutCurve Curve(double maximum)
        => new(
            [new PointCurveRequests.Sample(0, 0), new PointCurveRequests.Sample(1, maximum)]);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConcurrentFirstPuts_ShouldUpsertOneRowAndQueueOnlyChangedSamples(bool identical)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new RecordingQueue();
        await using var provider = Services(database, queue);
        var gate = new OverrideReadGate(2);
        await using var firstContext = ContextWithGate(database, gate);
        await using var secondContext = ContextWithGate(database, gate);
        var scopes = provider.GetRequiredService<IServiceScopeFactory>();
        var first = new PointCurveService(firstContext, TimeProvider.System, queue, scopes,
            provider.GetRequiredService<HybridCache>());
        var second = new PointCurveService(secondContext, TimeProvider.System, queue, scopes,
            provider.GetRequiredService<HybridCache>());

        var firstPut = first.PutAsync(_contextId, _mapId, _pointId, Curve(0.4), CancellationToken.None);
        var secondPut = second.PutAsync(
            _contextId, _mapId, _pointId, Curve(identical ? 0.4 : 0.8), CancellationToken.None);

        try
        {
            await gate.WaitForReadersAsync();
            gate.Release();

            var results = await Task.WhenAll(firstPut, secondPut);

            results.Should().OnlyContain(result => result.IsSuccess);
            queue.Count.Should().Be(identical ? 1 : 2);

            var stored = await database.Context.RankedMapPointCurves
                .SingleAsync();

            stored.Curve.Points[0].Y.Should().BeOneOf(0.4, identical ? 0.4 : 0.8);
            stored.Curve.Points[1].Should().Be(new CurvePoint(0, 0));

            (await first.PutAsync(_contextId, _mapId, _pointId, Curve(0.6), CancellationToken.None))
                .IsSuccess.Should().BeTrue();

            (await database.Context.RankedMapPointCurves
                .SingleAsync()).Curve.Points[0].Y.Should().Be(0.6);
            queue.Count.Should().Be(identical ? 2 : 3);
        }
        finally
        {
            gate.Release();
        }
    }

    [Test]
    public async Task PutAfterAnOverlappingEdit_ShouldQueueReweightEvenWhenItsInitialReadMatched()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new RecordingQueue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var current = scope.ServiceProvider.GetRequiredService<PointCurveService>();

        await current.PutAsync(_contextId, _mapId, _pointId, Curve(0.4), CancellationToken.None);

        var gate = new OverrideReadGate(1);
        await using var delayedContext = ContextWithGate(database, gate);
        var delayed = new PointCurveService(
            delayedContext, TimeProvider.System, queue, provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<HybridCache>());
        var delayedPut = delayed.PutAsync(_contextId, _mapId, _pointId, Curve(0.4), CancellationToken.None);

        try
        {
            await gate.WaitForReadersAsync();

            (await current.PutAsync(_contextId, _mapId, _pointId, Curve(0.8), CancellationToken.None))
                .IsSuccess.Should().BeTrue();

            gate.Release();

            (await delayedPut).IsSuccess.Should().BeTrue();
            (await database.Context.RankedMapPointCurves
                .SingleAsync()).Curve.Points[0].Y.Should().Be(0.4);
            queue.Count.Should().Be(3);
        }
        finally
        {
            gate.Release();
        }
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task UnchangedGeneratedSamples_ShouldStillPublishNewBenchmarkMetadata()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var point = await database.Context.Points
            .AsTracking()
            .SingleAsync();

        point.CurveSettings = point.CurveSettings with
        {
            MapCurvePolicy = PointCurvePolicy.ScoreDerivedMapCurve,
            RunnerUpRewardFractions = [0.8, 0.6, 0.4]
        };

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var queue = new RecordingQueue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        await database.AddScoreAsync(1, 1000);
        await scores.RecalculatePlayerScoresForMapAsync(
            _mapId, _pointId, new PlayerId(1), token: CancellationToken.None);

        (await curves.RefreshAsync(_mapId, _pointId, CancellationToken.None)).Should().BeTrue();

        var original = await database.Context.RankedMapPointCurves
            .SingleAsync();

        await database.AddPlayerAsync(2);
        await database.AddScoreAsync(2, 1000, playerId: 2);
        await scores.RecalculatePlayerScoresForMapAsync(
            _mapId, _pointId, new PlayerId(2), token: CancellationToken.None);

        (await curves.RefreshAsync(_mapId, _pointId, CancellationToken.None)).Should().BeFalse();

        var updated = await database.Context.RankedMapPointCurves
            .SingleAsync();

        updated.Curve.Points.Should().Equal(original.Curve.Points);
        updated.BenchmarkAccuracies.Should().Equal(1, 1);
        queue.Count.Should().Be(0);
    }
}
