using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.PointCurves.Http;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Database.Models.Server.Guilds;
using GuildSaber.Database.Models.Server.Guilds.Points;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Context = GuildSaber.Database.Models.Server.Guilds.Context;

namespace GuildSaber.Common.UnitTests.Features.PointCurves;

[PublicAPI]
public class PointCurveCacheRegressionTests
{
    private sealed class ContextQueryCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"ContextMembers\"")
                && command.CommandText.Contains("\"Contexts\"")
                && command.CommandText.Contains("\"Points\""))
                Count++;

            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingQueue : IHeavyBackgroundTaskQueue
    {
        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
            => ValueTask.CompletedTask;

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private static ServiceProvider Services(PointCurveTestDatabase database, ContextQueryCounter counter)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHeavyBackgroundTaskQueue>(new RecordingQueue())
            .AddScoped(_ => database.CreateContext(counter))
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .AddTransient<MapPointCurvePipeline>()
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .BuildServiceProvider();

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task RepeatedScores_ShouldLoadPlayerContextsOnceAndRefreshAfterExplicitInvalidation()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var score = await database.AddScoreAsync(1, 600);
        var counter = new ContextQueryCounter();
        await using var provider = Services(database, counter);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();

        for (var index = 0; index < 2; index++)
            await scores.ExecuteAsync(score, token: CancellationToken.None);

        counter.Count.Should().Be(1);

        var point = await database.Context.Points
            .AsTracking()
            .SingleAsync();

        database.Context.AddRange(
            new Context
            {
                Id = new ContextId(3),
                GuildId = new GuildId(2),
                Info = new ContextInfo("New context", ""),
                Points = [point]
            },
            new ContextMember
            {
                ContextId = new ContextId(3),
                GuildId = new GuildId(2),
                PlayerId = new PlayerId(1)
            });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        (await scores.ExecuteAsync(score, token: CancellationToken.None))
            .ImpactedContextsWithPoints.Should().ContainSingle();
        counter.Count.Should().Be(1);

        await scores.ClearPlayerCacheAsync(new PlayerId(1));
        var refreshed = await scores.ExecuteAsync(score, token: CancellationToken.None);
        ContextId[] expectedContexts = [new(2), new(3)];

        refreshed.ImpactedContextsWithPoints
            .Select(x => x.Id)
            .Should().BeEquivalentTo(expectedContexts);
        counter.Count.Should().Be(2);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task SharedSettingsEdit_ShouldInvalidateCachedPointsBeforeScoresAreProcessedAgain()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var score = await database.AddScoreAsync(1, 600);
        var counter = new ContextQueryCounter();
        await using var provider = Services(database, counter);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();

        await scores.ExecuteAsync(score, token: CancellationToken.None);

        counter.Count.Should().Be(1);

        (await curves.UpdateSettingsAsync(new GuildId(2), new Point.PointId(3),
                new PointCurveRequests.UpdateSettings(
                    [new PointCurveRequests.Sample(0, 100), new PointCurveRequests.Sample(32, 100)],
                    [new PointCurveRequests.Sample(0, 0), new PointCurveRequests.Sample(1, 1)], 0.5,
                    new PointCurveRequests.Policy.EditableMapCurve()), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        await scores.ExecuteAsync(score, token: CancellationToken.None);

        counter.Count.Should().Be(2);
        ((float)(await database.Context.PointGivingRankedScores.SingleAsync()).RawPoints).Should().Be(130);
    }

    [Test]
    [UsedImplicitly(Reason = "TUnit invokes this method on a test instance.")]
    public async Task MapCurveEdit_ShouldUseTheNewShapeWithoutDiscardingThePlayerContextCache()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        var score = await database.AddScoreAsync(1, 600);
        var counter = new ContextQueryCounter();
        await using var provider = Services(database, counter);
        await using var scope = provider.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoreAddOrUpdatePipeline>();
        var curves = scope.ServiceProvider.GetRequiredService<PointCurveService>();

        await scores.ExecuteAsync(score, token: CancellationToken.None);
        await curves.PutAsync(new ContextId(2), new RankedMapId(1), new Point.PointId(3),
            new PointCurveRequests.PutCurve(
                [new PointCurveRequests.Sample(0, 0), new PointCurveRequests.Sample(0.6, 0.4),
                    new PointCurveRequests.Sample(1, 1)]), CancellationToken.None);
        await scores.ExecuteAsync(score, token: CancellationToken.None);

        counter.Count.Should().Be(1);
        ((float)(await database.Context.PointGivingRankedScores.SingleAsync()).RawPoints).Should().Be(110);
    }

}
