using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using GuildSaber.Database.Models.Server.RankedScores;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Color = System.Drawing.Color;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveSelectionRegressionTests
{
    private static readonly GuildId _guild = new(2);
    private static readonly ContextId _context = new(2);
    private static readonly RankedMapId _map = new(1);
    private static readonly Point.PointId _point = new(3);

    private static readonly CurvePoint[] _acceptedWins = [(1, 0), (0.9, 0), (0.6, 1), (0, 0)];
    private static readonly CurvePoint[] _pendingWins = [(1, 1), (0.9, 1), (0.6, 0), (0, 0)];

    private sealed class Queue : IHeavyBackgroundTaskQueue
    {
        private readonly Queue<Func<CancellationToken, ValueTask>> _items = new();

        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> item, CancellationToken token = default)
        {
            _items.Enqueue(item);

            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken token)
            => ValueTask.FromResult(_items.Dequeue());

        public async Task DrainAsync()
        {
            while (_items.TryDequeue(out var item))
                await item(CancellationToken.None);
        }
    }

    private sealed class RankCommands : DbCommandInterceptor
    {
        public List<Update> Updates { get; } = [];
        public bool Capture { get; set; }
        public sealed record Update(int? PointId);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Capture || !command.CommandText.Contains("DENSE_RANK()", StringComparison.Ordinal))
                return ValueTask.FromResult(result);

            var pointId = command.Parameters.Count > 1 && command.Parameters[1].Value is int id
                ? id
                : (int?)null;

            Updates.Add(new Update(pointId));

            return ValueTask.FromResult(result);
        }
    }

    private static ServiceProvider Services(
        PointCurveTestDatabase database,
        Queue queue,
        RankCommands? commands = null)
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
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .BuildServiceProvider();

    private static PointCurveRequests.Sample[] Samples(CurvePoint[] points)
        => Array.ConvertAll(points, point => new PointCurveRequests.Sample(point.X, point.Y));

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task CurveSelectionChange_ShouldRefreshAchievementsAndKeepCandidateApprovals(bool pointwideSettings)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddScoreAsync(1, 600);
        await database.AddScoreAsync(2, 900);

        var db = database.Context;

        await db.RankedMaps
            .Where(x => x.Id == _map)
            .ExecuteUpdateAsync(x => x.SetProperty(map => map.Requirements.NeedConfirmation, true));

        db.Achievements.Add(new DiffStarAchievement(
            new Achievement.AchievementId(1), _guild, _context, null,
            new AchievementInfo("One accepted pass", Color.White), default, null, false, default,
            new RankedMapRating.DifficultyStar(0), 1));

        if (pointwideSettings)
        {
            var point = await db.Points
                .AsTracking()
                .SingleAsync(x => x.Id == _point);

            point.CurveSettings = point.CurveSettings with { Accuracy = new CustomCurve(_acceptedWins) };
        }
        else
        {
            db.RankedMapPointCurves.Add(new RankedMapPointCurve
            {
                RankedMapId = _map,
                PointId = _point,
                Curve = new CustomCurve(_acceptedWins),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var queue = new Queue();
        await using var provider = Services(database, queue);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();
        var achievements = scope.ServiceProvider.GetRequiredService<MemberAchievementStatsPipeline>();

        await scores.RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: CancellationToken.None);

        await db.Database.ExecuteSqlAsync($"""
                                           UPDATE "RankedScores" SET "Type" = 3
                                           WHERE "PointId" = {_point.Value} AND "ScoreId" = 1
                                           """);

        await scores.RecalculatePlayerScoresForMapAsync(_map, _point, new PlayerId(1), token: CancellationToken.None);
        await achievements.ExecuteAsync(new PlayerId(1), _guild, _context, _point);

        var initial = await db.MemberAchievementStats
            .SingleAsync();

        initial.PassCount.Should().Be(1);
        initial.IsCompleted.Should().BeTrue();

        await PublishAsync(_pendingWins);
        await queue.DrainAsync();

        (await db.RankedScores
                .Where(x => x.PointId == _point && x.IsSelected)
                .SingleAsync())
            .Should().BeOfType<PendingRankedScore>();

        var incomplete = await db.MemberAchievementStats
            .SingleAsync();

        incomplete.PassCount.Should().Be(0);
        incomplete.IsCompleted.Should().BeFalse();

        (await db.RankedScores
                .Where(x => x.PointId == _point && x.ScoreId == new ScoreId(1))
                .SingleAsync())
            .Should().BeOfType<AcceptedRankedScore>();

        await PublishAsync(_acceptedWins);
        await queue.DrainAsync();

        var restored = await db.MemberAchievementStats
            .SingleAsync();

        restored.PassCount.Should().Be(1);
        restored.IsCompleted.Should().BeTrue();

        (await db.RankedScores
                .Where(x => x.PointId == _point && x.ScoreId == new ScoreId(2))
                .SingleAsync())
            .Should().BeOfType<PendingRankedScore>();

        return;

        async Task PublishAsync(CurvePoint[] points)
        {
            if (pointwideSettings)
                (await curves.UpdateSettingsAsync(_guild, _point,
                        new PointCurveRequests.UpdateSettings(
                            [new PointCurveRequests.Sample(0, 100), new PointCurveRequests.Sample(32, 100)],
                            Samples(points), 0.25,
                            new PointCurveRequests.Policy.EditableMapCurve()), CancellationToken.None))
                    .IsSuccess.Should().BeTrue();
            else
                (await curves.PutAsync(_context, _map, _point,
                        new PointCurveRequests.PutCurve(Samples(points)), CancellationToken.None))
                    .IsSuccess.Should().BeTrue();
        }
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task MapBatch_ShouldRankOnceAfterBothNativeScoringPassesAndLeaveOtherPointsUntouched()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();

        for (var player = 2; player <= 3; player++)
            await database.AddPlayerAsync(player);

        for (var player = 1; player <= 3; player++)
            await database.AddScoreAsync(player, 700 - player * 100, playerId: player);

        var db = database.Context;
        var context = await db.Contexts
            .AsTracking()
            .Include(x => x.Points)
            .SingleAsync(x => x.Id == _context);

        var point = context.Points.Single(x => x.Id == _point);
        point.CurveSettings = point.CurveSettings with
        {
            MapCurvePolicy = PointCurvePolicy.ScoreDerivedMapCurve,
            RunnerUpRewardFractions = [0.8, 0.6, 0.4]
        };

        var other = new Point
        {
            Id = new Point.PointId(4),
            GuildId = _guild,
            Info = PointInfo.TryCreate("Other", "Unrelated ranks").Value,
            CurveSettings = new CurveSettings
            {
                Difficulty = new CustomCurve([(32, 100), (0, 100)]),
                Accuracy = new CustomCurve([(1, 1), (0, 0)])
            }
        };

        db.Points.Add(other);
        context.Points.Add(other);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var queue = new Queue();
        var commands = new RankCommands();
        await using var provider = Services(database, queue, commands);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var pipeline = scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>();

        for (var player = 1; player <= 3; player++)
            await scores.RecalculatePlayerScoresForMapAsync(
                _map, other.Id, new PlayerId(player), token: CancellationToken.None);

        await db.PointGivingRankedScores
            .Where(x => x.PointId == other.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(score => score.Rank, 77));

        commands.Capture = true;

        await pipeline.ExecuteAsync(_map, _point, regenerate: true, token: CancellationToken.None);

        commands.Updates.Should().ContainSingle().Which.PointId.Should().Be(_point.Value);

        var selected = await db.PointGivingRankedScores
            .Where(x => x.PointId == _point && x.IsSelected)
            .OrderBy(x => x.Rank)
            .Select(x => new { x.Rank, x.RawPoints })
            .ToArrayAsync();

        selected
            .Select(x => x.Rank)
            .Should().Equal(1, 2, 3);
        selected
            .Select(x => (float)x.RawPoints)
            .Should().Equal(125, 120, 115);

        (await db.PointGivingRankedScores
                .Where(x => x.PointId == other.Id)
                .Select(x => x.Rank)
                .ToArrayAsync())
            .Should().OnlyContain(rank => rank == 77);
    }
}