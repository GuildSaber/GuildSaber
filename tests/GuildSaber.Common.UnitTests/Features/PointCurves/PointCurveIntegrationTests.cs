using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Contexts.Server;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.MapRequests.Proposals;
using GuildSaber.Database.Models.Server.MapRequests.Proposals.Types;
using GuildSaber.Database.Models.Server.RankedScores;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using JetBrains.Annotations;
using Sample = GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests.Sample;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveIntegrationTests
{
    private static readonly GuildId _guild = new(2);
    private static readonly ContextId _context = new(2);
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
            while (_items.TryDequeue(out var item))
                await item(CancellationToken.None);
        }
    }

    private static ServiceProvider Services(PointCurveTestDatabase database, IHeavyBackgroundTaskQueue queue)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton(queue)
            .AddScoped(_ => database.CreateContext())
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .AddTransient<MapPointCurvePipeline>()
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .BuildServiceProvider();

    private static PointCurveRequests.UpdateSettings Settings(PointCurveRequests.Policy policy, double ratio = 0.25)
        => new([new Sample(0, 100), new Sample(32, 100)], [new Sample(0, 0), new Sample(1, 1)], ratio, policy);

    [Test]
    public async Task MinimumAccuracy_ShouldRetainDoublePrecisionInMigratedMapsAndProposals()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync(useMigrations: true);
        var accuracy = Accuracy.TryCreate(95.123456789123).Value;
        var map = await database.Context.RankedMaps
            .AsTracking()
            .SingleAsync();

        map.Requirements = map.Requirements with { MinAccuracy = accuracy };
        database.Context.MapProposals.Add(new DraftProposal
        {
            ThreadId = map.ThreadId,
            CreatedAt = DateTimeOffset.UtcNow,
            Requirements = map.Requirements,
            Rating = new MapProposalRating()
        });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await database.Context.RankedMaps
            .SingleAsync()).Requirements.MinAccuracy.Should().Be(accuracy);
        (await database.Context.MapProposals
            .SingleAsync()).Requirements.MinAccuracy.Should().Be(accuracy);
    }

    [Test]
    public async Task ManualPublication_ShouldReweightOnlyAfterQueuedWorkAndRestoreFallbackOnDelete()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        var score = await database.AddScoreAsync(1, 600);

        await scores.ExecuteAsync(score, token: CancellationToken.None);

        ((float)(await database.Context.ScoredRankedScores
            .SingleAsync()).RawPoints).Should().Be(115);

        var shape = new PointCurveRequests.PutCurve([new Sample(0, 0), new Sample(0.6, 0.4), new Sample(1, 1)]);
        var saved = await curves.PutAsync(_context, _map, _point, shape, CancellationToken.None);

        saved.IsSuccess.Should().BeTrue();
        saved.Value.IsMapOverride.Should().BeTrue();

        (await curves.PutAsync(_context, _map, _point, shape, CancellationToken.None)).IsSuccess.Should().BeTrue();

        queue.Count.Should().Be(1);
        ((float)(await database.Context.ScoredRankedScores
            .SingleAsync()).RawPoints).Should().Be(115);

        await queue.DrainAsync();

        ((float)(await database.Context.ScoredRankedScores
            .SingleAsync()).RawPoints).Should().Be(110);
        (await database.Context.MemberPointStats
            .SingleAsync()).Points.Should().Be(110);

        (await curves.DeleteAsync(_context, _map, _point, CancellationToken.None))
            .Value.IsMapOverride.Should().BeFalse();
        (await curves.DeleteAsync(_context, _map, _point, CancellationToken.None))
            .Value.IsMapOverride.Should().BeFalse();

        queue.Count.Should().Be(1);

        await queue.DrainAsync();

        ((float)(await database.Context.ScoredRankedScores
            .SingleAsync()).RawPoints).Should().Be(115);
        (await database.Context.RankedMapPointCurves
            .CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task NativeCurve_ShouldUseDistinctPlayersAndCorrectExistingPlayersAfterNewTopPass()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();

        for (var player = 2; player <= 5; player++)
            await database.AddPlayerAsync(player);

        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        (await curves.UpdateSettingsAsync(_guild, _point,
            Settings(new PointCurveRequests.Policy.ScoreDerivedMapCurve([0.8, 0.6, 0.4])), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        for (var player = 1; player <= 4; player++)
            await database.AddScoreAsync(player, 700 - player * 100, playerId: player);
        await database.AddScoreAsync(6, 590); // Same player must not consume a runner-up slot.

        await queue.DrainAsync();

        var original = await database.Context.RankedMapPointCurves
            .SingleAsync();

        original.BenchmarkAccuracies.Should().Equal(0.6, 0.5, 0.4, 0.3);
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(1) && x.IsSelected))
            .RawPoints).Should().Be(125);
        (await curves.PutAsync(_context, _map, _point,
                new PointCurveRequests.PutCurve([new Sample(0, 0), new Sample(1, 1)]), CancellationToken.None))
            .Error.Should().BeOfType<PointCurveError.ReadOnly>();

        var newTop = await database.AddScoreAsync(7, 800, playerId: 5);

        await scores.ExecuteAsync(newTop, token: CancellationToken.None);
        queue.Count.Should().Be(1);

        // Ordinary ingestion used the existing curve. The other players are still at their previous values.
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(1) && x.IsSelected))
            .RawPoints).Should().Be(125);

        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves
            .SingleAsync()).BenchmarkAccuracies.Should().Equal(0.8, 0.6, 0.5, 0.4);
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(1) && x.IsSelected))
            .RawPoints).Should().Be(120);
        (await database.Context.MemberPointStats
            .SingleAsync(x => x.PlayerId == new PlayerId(1))).Points.Should().Be(120);
        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(5))).RawPoints)
            .Should().Be(125);

        var updatedAt = (await database.Context.RankedMapPointCurves
            .SingleAsync()).UpdatedAt;

        await scores.ExecuteAsync(await database.AddScoreAsync(8, 200, playerId: 4), token: CancellationToken.None);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves
            .SingleAsync()).UpdatedAt.Should().Be(updatedAt);
    }

    [Test]
    public async Task Confirmation_ShouldRemainPerPointAndFollowExistingRequirementInvalidation()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();

        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Requirements.NeedConfirmation, true));

        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        await database.AddScoreAsync(1, 600);
        await scores.RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: CancellationToken.None);
        await database.Context.Database.ExecuteSqlRawAsync(
            "UPDATE \"RankedScores\" SET \"Type\" = 3 WHERE \"PointId\" = 3");

        var other = new Point
        {
            Id = new Point.PointId(4),
            GuildId = _guild,
            Info = PointInfo.TryCreate("Other", "Independent approval fixture").Value,
            CurveSettings = new CurveSettings
            {
                Difficulty = new CustomCurve([(32, 100), (0, 100)]),
                Accuracy = new CustomCurve([(1, 1), (0, 0)])
            }
        };

        var context = await database.Context.Contexts
            .AsTracking()
            .Include(x => x.Points)
            .SingleAsync();

        database.Context.Points.Add(other);
        context.Points.Add(other);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        await scores.RecalculatePlayerScoresForMapAsync(_map, other.Id, new PlayerId(1), token: CancellationToken.None);
        (await database.Context.RankedScores
            .SingleAsync(x => x.PointId == other.Id)).Should().BeOfType<PendingRankedScore>();
        (await database.Context.RankedScores
            .SingleAsync(x => x.PointId == _point)).Should().BeOfType<AcceptedRankedScore>();

        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Requirements.NeedFullCombo, true));
        await scores.RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: CancellationToken.None);
        (await database.Context.RankedScores
            .SingleAsync(x => x.PointId == _point)).Should().BeOfType<InvalidRankedScore>();

        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Requirements.NeedFullCombo, false));
        await scores.RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: CancellationToken.None);
        (await database.Context.RankedScores
            .SingleAsync(x => x.PointId == _point)).Should().BeOfType<PendingRankedScore>();
    }

    [Test]
    public async Task Settings_ShouldRoundTripWithinCurveSettingsAndKeepShapeWhenOnlyBudgetChanges()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();

        await database.AddScoreAsync(1, 600);
        await curves.PutAsync(_context, _map, _point,
            new PointCurveRequests.PutCurve([new Sample(0, 0), new Sample(0.6, 0.4), new Sample(1, 1)]),
            CancellationToken.None);
        await queue.DrainAsync();

        var saved = await curves.UpdateSettingsAsync(_guild, _point,
            Settings(new PointCurveRequests.Policy.EditableMapCurve(), 0.5), CancellationToken.None);

        saved.IsSuccess.Should().BeTrue();
        (await database.Context.Points
            .SingleAsync()).CurveSettings.AccuracyBonusRatio.Should().Be(0.5);
        (await database.Context.RankedMapPointCurves
            .CountAsync()).Should().Be(1);

        await queue.DrainAsync();

        ((float)(await database.Context.ScoredRankedScores
            .SingleAsync()).RawPoints).Should().Be(120);
    }

    [Test]
    public async Task ReactivationAndDeletion_ShouldRefreshBenchmarksAndClearLastPassTotals()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync(useMigrations: true);
        await database.AddPlayerAsync(2);

        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var policy = new PointCurveRequests.Policy.ScoreDerivedMapCurve([0.8, 0.6, 0.4]);
        var top = await database.AddScoreAsync(1, 600);

        await curves.UpdateSettingsAsync(_guild, _point, Settings(policy), CancellationToken.None);
        await queue.DrainAsync();

        await curves.UpdateSettingsAsync(_guild, _point, Settings(policy, 0), CancellationToken.None);
        await queue.DrainAsync();

        await scores.ExecuteAsync(await database.AddScoreAsync(2, 800, playerId: 2), token: CancellationToken.None);
        queue.Count.Should().Be(0);
        (await database.Context.RankedMapPointCurves
            .SingleAsync()).BenchmarkAccuracies.Should().Equal(0.6);

        await curves.UpdateSettingsAsync(_guild, _point, Settings(policy), CancellationToken.None);
        await queue.DrainAsync();

        (await database.Context.RankedMapPointCurves
            .SingleAsync()).BenchmarkAccuracies.Should().Equal(0.8, 0.6);

        await new ScoreDeletionPipeline(scope.ServiceProvider.GetRequiredService<ServerDbContext>(), curves)
            .ExecuteAsync(top);
        await queue.DrainAsync();

        (await database.Context.MemberPointStats
            .SingleAsync(x => x.PlayerId == new PlayerId(1))).Points.Should().Be(0);
        (await database.Context.RankedMapPointCurves
            .SingleAsync()).BenchmarkAccuracies.Should().Equal(0.8);
    }

    [Test]
    public async Task HeavyBackgroundWork_ShouldNotWaitForSpaceInItsOwnQueue()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new HeavyBackgroundTaskQueue(1);
        await using var provider = Services(database, queue);
        using var worker = new HeavyQueueProcessingService(queue, NullLogger<HeavyQueueProcessingService>.Instance);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await queue.QueueBackgroundWorkItemAsync(async token =>
        {
            try
            {
                await using var scope = provider.CreateAsyncScope();
                var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();

                for (var i = 0; i < 3; i++)
                    await curves.QueueRefreshAsync(_map, _point, token);

                completed.SetResult();
            }
            catch (Exception error)
            {
                completed.SetException(error);
            }
        });

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task Benchmarks_ShouldIgnoreFailedUnapprovedAndDepartedPlayersWithoutChangingApprovalState()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();

        for (var player = 2; player <= 5; player++)
            await database.AddPlayerAsync(player);

        await database.Context.RankedMaps
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.Requirements.NeedConfirmation, true));

        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        for (var player = 1; player <= 5; player++)
        {
            await database.AddScoreAsync(
                player, player == 1 ? 600 : 900 + player, playerId: player, isWin: player != 2);
            await scores.RecalculatePlayerScoresForMapAsync(
                _map, _point, new PlayerId(player), token: CancellationToken.None);
        }

        await database.Context.Database.ExecuteSqlRawAsync("""
            UPDATE "RankedScores" SET "Type" = CASE WHEN "PlayerId" IN (1,2,5) THEN 3 WHEN "PlayerId" = 4 THEN 4 ELSE 2 END;
            UPDATE "Points" SET "CurveSettings_MapCurvePolicy" = 2, "CurveSettings_RunnerUpRewardFractions" = ARRAY[0.8,0.6,0.4]::double precision[];
            DELETE FROM "ContextMembers" WHERE "PlayerId" = 5;
            """);

        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();

        (await curves.RefreshAsync(_map, _point, CancellationToken.None)).Should().BeTrue();
        (await database.Context.RankedMapPointCurves
            .SingleAsync()).BenchmarkAccuracies.Should().Equal(0.6);
        (await database.Context.RankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(2))).Should().BeOfType<AcceptedRankedScore>();
        (await database.Context.RankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(3))).Should().BeOfType<PendingRankedScore>();
        (await database.Context.RankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(4))).Should().BeOfType<RefusedRankedScore>();
    }

    [Test]
    public async Task SettingsTransaction_ShouldSupportTheProductionRetryStrategy()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var queue = new Queue();
        await using var provider = Services(database, queue);

        await using var retryContext = database.CreateContext(retry: true);

        var curves = new PointCurveService(
            retryContext, TimeProvider.System, queue, provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<HybridCache>());

        var updated = await curves.UpdateSettingsAsync(_guild, _point,
            Settings(new PointCurveRequests.Policy.ScoreDerivedMapCurve([0.8, 0.6, 0.4])), CancellationToken.None);

        updated.IsSuccess.Should().BeTrue();
        queue.Count.Should().Be(1);
        (await database.Context.Points
            .SingleAsync()).CurveSettings.MapCurvePolicy.Should().Be(PointCurvePolicy.ScoreDerivedMapCurve);
    }
}
