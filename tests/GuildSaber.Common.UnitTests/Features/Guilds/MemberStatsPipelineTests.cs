using System.Data.Common;
using AwesomeAssertions;
using GuildSaber.Api.Features.Guilds.Members.Pipelines;
using GuildSaber.Api.Features.PointCurves;
using GuildSaber.Api.Features.RankedMaps.Pipelines;
using GuildSaber.Api.Features.Scores.Pipelines;
using GuildSaber.Api.Features.Scores.Services;
using GuildSaber.Api.Queuing;
using GuildSaber.Common.Services.BeatLeader;
using GuildSaber.Common.Services.ScoreSaber;
using GuildSaber.Common.StrongTypes;
using GuildSaber.Common.UnitTests.Features.PointCurves;
using GuildSaber.Database.Models.Server.Guilds.Achievements;
using GuildSaber.Database.Models.Server.Guilds.Achievements.Types;
using GuildSaber.Database.Models.Server.Guilds.Points;
using GuildSaber.Database.Models.Server.RankedMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Color = System.Drawing.Color;
using SongDifficultyId = GuildSaber.Database.Models.Server.Songs.SongDifficulties.SongDifficulty.SongDifficultyId;

namespace GuildSaber.Common.UnitTests.Features.Guilds;

public class MemberStatsPipelineTests
{
    private static readonly RankedMapId _map = new(1);
    private static readonly Point.PointId _point = new(3);

    private sealed class UnusedQueue : IHeavyBackgroundTaskQueue
    {
        public ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem, CancellationToken token = default)
            => throw new InvalidOperationException("These fixtures do not publish native curves.");

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken token)
            => throw new NotSupportedException();
    }

    private sealed class StatsQueries : DbCommandInterceptor
    {
        public int CategoryReads { get; private set; }
        public int AchievementReads { get; private set; }

        public void Reset() => (CategoryReads, AchievementReads) = (0, 0);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Categories\"", StringComparison.Ordinal))
                CategoryReads++;

            if (command.CommandText.Contains("FROM \"Achievements\"", StringComparison.Ordinal))
                AchievementReads++;

            return ValueTask.FromResult(result);
        }
    }

    private static ServiceProvider Services(PointCurveTestDatabase database, StatsQueries queries)
        => new ServiceCollection()
            .AddLogging()
            .AddHybridCache()
            .Services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHeavyBackgroundTaskQueue>(new UnusedQueue())
            .AddSingleton(_ => new HttpClient())
            .AddTransient<BeatLeaderApi>()
            .AddTransient<ScoreSaberApi>()
            .AddScoped(_ => database.CreateContext(queries))
            .AddScoped<HmdResolverService>()
            .AddScoped<ControllerResolverService>()
            .AddScoped<PointCurveService>()
            .AddTransient<ScoreAddOrUpdatePipeline>()
            .AddTransient<MapPointCurvePipeline>()
            .AddTransient<MemberStatsPipeline>()
            .AddTransient<MemberPointStatsPipeline>()
            .AddTransient<MemberAchievementStatsPipeline>()
            .AddTransient<AddRankedMapPipeline>()
            .AddTransient<EditRankedMapPipeline>()
            .AddTransient<ApplyMapProposalPipeline>()
            .BuildServiceProvider();

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ProposalApplication_ShouldRefreshEachMemberOnceAfterScoring(bool addedVersion)
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddPlayerAsync(2);
        await database.AddScoreAsync(1, 600);
        await database.AddScoreAsync(2, 800, playerId: 2);

        var queries = new StatsQueries();
        await using var provider = Services(database, queries);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ApplyMapProposalPipeline>()
            .ExecuteAsync(_map, addedVersion ? [new SongDifficultyId(1)] : [], CancellationToken.None);

        // Each member refresh reads categories once and achievements once for this point.
        queries.CategoryReads.Should().Be(2);
        queries.AchievementReads.Should().Be(2);

        var stats = await database.Context.MemberPointStats
            .OrderBy(x => x.PlayerId)
            .ToArrayAsync();

        stats.Should().HaveCount(2);
        stats[0].Points.Should().Be(115);
        stats[1].Points.Should().Be(120);
        stats.Should().OnlyContain(x => x.PassCount == 1);
    }

    [Test]
    public async Task PointRefresh_ShouldClearFormerPassesWithoutRefreshingUnrelatedMembers()
    {
        await using var database = await PointCurveTestDatabase.CreateAsync();
        await database.AddPlayerAsync(2);
        await database.AddScoreAsync(1, 600);

        database.Context.Achievements.Add(new DiffStarAchievement(
            new Achievement.AchievementId(1), new GuildId(2), new ContextId(2), null,
            new AchievementInfo("One pass", Color.White), default, null, false, default,
            new RankedMapRating.DifficultyStar(0), 1));

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var queries = new StatsQueries();
        await using var provider = Services(database, queries);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<MapPointCurvePipeline>()
            .ExecuteAsync(_map, _point, regenerate: false, token: CancellationToken.None);

        (await database.Context.MemberAchievementStats.SingleAsync()).IsCompleted.Should().BeTrue();

        await database.Context.Scores.ExecuteDeleteAsync();
        queries.Reset();

        await scope.ServiceProvider.GetRequiredService<MemberStatsPipeline>()
            .RecalculateForPointAsync(_point, CancellationToken.None);

        queries.CategoryReads.Should().Be(1);
        queries.AchievementReads.Should().Be(1);

        var stats = await database.Context.MemberPointStats.SingleAsync();

        stats.PlayerId.Should().Be(new PlayerId(1));
        stats.Points.Should().Be(0);
        stats.PassCount.Should().Be(0);

        var achievement = await database.Context.MemberAchievementStats.SingleAsync();

        achievement.PassCount.Should().Be(0);
        achievement.IsCompleted.Should().BeFalse();
    }
}