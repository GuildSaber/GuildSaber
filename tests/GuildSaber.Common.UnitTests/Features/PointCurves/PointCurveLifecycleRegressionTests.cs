using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.Players.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.MapRequests;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions;
using GuildSaber.Database.Models.Server.RankedMaps.MapVersions.PlayModes;
using GuildSaber.Database.Models.Server.Songs;
using GuildSaber.Database.Models.Server.Songs.SongDifficulties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EModifiers = GuildSaber.Database.Models.Server.Scores.AbstractScore.EModifiers;
using JetBrains.Annotations;
using Sample = GuildSaber.Api.Features.PointCurves.Http.PointCurveRequests.Sample;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveLifecycleRegressionTests
{
    private sealed class RecordingQueue : IHeavyBackgroundTaskQueue
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
            .AddTransient<PlayerDeletionPipeline>()
            .BuildServiceProvider();

    [Test]
    public async Task InvalidOversizedScore_ShouldNotAbortPointWideRecalculation()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var original = await database.Context.RankedMaps.SingleAsync();
        var now = DateTimeOffset.UtcNow;

        database.Context.AddRange(
            new MapRequestThread
            {
                Id = new MapRequestThreadId(2),
                ContextId = new ContextId(2),
                CreatedAt = now
            },
            new RankedMap
            {
                Id = new RankedMapId(2),
                GuildId = original.GuildId,
                ContextId = original.ContextId,
                ThreadId = new MapRequestThreadId(2),
                Info = new RankedMapInfo(now, now),
                Rating = original.Rating with { },
                Requirements = original.Requirements with { }
            },
            new MapVersion
            {
                RankedMapId = new RankedMapId(2),
                SongDifficultyId = new SongDifficulty.SongDifficultyId(1),
                SongId = new Song.SongId(1),
                PlayModeId = new PlayMode.PlayModeId(1),
                AddedAt = now
            });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        await database.AddScoreAsync(1, 1200, EModifiers.NoFail);
        await database.AddScoreAsync(2, 600);

        var queue = new RecordingQueue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var settings = new PointCurveRequests.UpdateSettings(
            [new Sample(0, 100), new Sample(32, 100)], [new Sample(0, 0), new Sample(1, 1)], 0.5,
            new PointCurveRequests.Policy.EditableMapCurve());

        (await curves.UpdateSettingsAsync(new GuildId(2), new Point.PointId(3), settings, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        await queue.DrainAsync();

        (await database.Context.InvalidRankedScores.CountAsync()).Should().Be(2);
        (await database.Context.PointGivingRankedScores.CountAsync()).Should().Be(2);
        var rewards = await database.Context.PointGivingRankedScores
            .Select(x => (float)x.RawPoints)
            .ToArrayAsync();

        rewards.Should().OnlyContain(x => x.Equals(130f));
        (await database.Context.MemberPointStats.SingleAsync()).Points.Should().Be(260);
    }

    [Test]
    public async Task DeletingBenchmarkPlayer_ShouldRefreshRemainingPlayersAndMapRanks()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddPlayerAsync(2);
        await database.AddScoreAsync(1, 800);
        await database.AddScoreAsync(2, 600, playerId: 2);

        var queue = new RecordingQueue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var settings = new PointCurveRequests.UpdateSettings(
            [new Sample(0, 100), new Sample(32, 100)], [new Sample(0, 0), new Sample(1, 1)], 0.25,
            new PointCurveRequests.Policy.ScoreDerivedMapCurve([0.8, 0.6, 0.4]));

        await curves.UpdateSettingsAsync(new GuildId(2), new Point.PointId(3), settings, CancellationToken.None);
        await queue.DrainAsync();

        ((float)(await database.Context.PointGivingRankedScores
            .SingleAsync(x => x.PlayerId == new PlayerId(2))).RawPoints).Should().Be(120);
        var deletion = scope.ServiceProvider.GetRequiredService<PlayerDeletionPipeline>();

        (await deletion.ExecuteAsync(new PlayerId(1), CancellationToken.None)).Should().BeTrue();
        queue.Count.Should().Be(1);
        await queue.DrainAsync();

        var remaining = await database.Context.PointGivingRankedScores.SingleAsync();

        ((float)remaining.RawPoints).Should().Be(125);
        remaining.Rank.Should().Be(1);
        (await database.Context.MemberPointStats.SingleAsync()).Points.Should().Be(125);
        (await database.Context.RankedMapPointCurves.SingleAsync()).BenchmarkAccuracies.Should().Equal(0.6);
        (await deletion.ExecuteAsync(new PlayerId(1), CancellationToken.None)).Should().BeFalse();
        queue.Count.Should().Be(0);
    }
}
